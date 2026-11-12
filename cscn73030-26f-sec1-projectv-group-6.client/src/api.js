/**
 * @file api.js
 * @brief Client for the CMES Staff/HR API (see API.md at the repository root).
 *
 * Every call goes to the same origin. In development Vite forwards the API paths to the
 * ASP.NET server (vite.config.js); in production the server hosts this app itself.
 * Errors come back as RFC 7807 ProblemDetails and are thrown as ApiError.
 */

/** An HTTP error from the API, carrying the ProblemDetails body. */
export class ApiError extends Error {
  constructor(status, problem) {
    super(problemMessage(status, problem));
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem || {};
    /** Field errors from a 400/422 response, e.g. { email: ['...'] }. */
    this.errors = problem?.errors || {};
  }
}

function problemMessage(status, problem) {
  if (!problem) return status ? `The server answered ${status}.` : 'The Staff/HR API could not be reached.';
  const fields = Object.values(problem.errors || {}).flat();
  if (fields.length) return fields.join(' ');
  return problem.detail || problem.title || `The server answered ${status}.`;
}

/** Builds "?a=1&b=2", leaving out empty values. */
export function toQuery(params = {}) {
  const query = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== '') query.append(key, String(value));
  }
  const text = query.toString();
  return text ? '?' + text : '';
}

async function request(method, path, { query, body, headers } = {}) {
  let response;
  try {
    response = await fetch(path + toQuery(query), {
      method,
      headers: { Accept: 'application/json', ...(body === undefined ? {} : { 'Content-Type': 'application/json' }), ...headers },
      body: body === undefined ? undefined : JSON.stringify(body),
    });
  } catch {
    throw new ApiError(0, null);
  }
  const text = response.status === 204 ? '' : await response.text();
  let data = null;
  if (text) {
    try { data = JSON.parse(text); } catch { data = { detail: text }; }
  }
  if (!response.ok) throw new ApiError(response.status, data);
  return data;
}

const get = (path, query) => request('GET', path, { query });
const post = (path, body, headers) => request('POST', path, { body, headers });
const patch = (path, body) => request('PATCH', path, { body });
const put = (path, body) => request('PUT', path, { body });
const del = (path) => request('DELETE', path);
const seg = encodeURIComponent;

/** A fresh value for the Idempotency-Key header. */
export const newIdempotencyKey = () =>
  globalThis.crypto?.randomUUID?.() ?? 'key-' + Date.now().toString(36) + '-' + Math.random().toString(36).slice(2);

export const api = {
  // Departments and services
  departments: () => get('/departments'),
  services: (code) => get(`/departments/${seg(code)}/services`),
  departmentHeadcount: () => get('/departments/headcount'),

  // Staff
  searchStaff: (query) => get('/staff', query),
  staff: (staffNumber) => get(`/staff/${seg(staffNumber)}`),
  createStaff: (body) => post('/staff', body),
  updateStaff: (staffNumber, body) => patch(`/staff/${seg(staffNumber)}`, body),

  // Assignments
  assignments: (staffNumber) => get(`/staff/${seg(staffNumber)}/assignments`),
  addAssignment: (staffNumber, body) => post(`/staff/${seg(staffNumber)}/assignments`, body),
  updateAssignment: (id, body) => patch(`/assignments/${id}`, body),

  // Rosters
  rosters: (query) => get('/rosters', query),
  addRosterEntry: (body) => post('/rosters/entries', body),
  deleteRosterEntry: (id) => del(`/rosters/entries/${id}`),
  rosterHeadcount: (query) => get('/rosters/headcount', query),
  onDuty: (query) => get('/rosters/on-duty', query),

  // Requisitions
  requisitions: (query) => get('/requisitions', query),
  requisition: (number) => get(`/requisitions/${seg(number)}`),
  raiseRequisition: (body, idempotencyKey = newIdempotencyKey()) =>
    post('/requisitions', body, { 'Idempotency-Key': idempotencyKey }),
  updateRequisition: (number, body) => patch(`/requisitions/${seg(number)}`, body),

  // Credentials
  credentials: (query) => get('/credentials', query),
  credentialTypes: () => get('/credentials/types'),
  expiringCredentials: (withinDays) => get('/credentials/expiring', { withinDays }),
  addCredential: (staffNumber, body) => post(`/staff/${seg(staffNumber)}/credentials`, body),
  updateCredential: (id, body) => patch(`/credentials/${id}`, body),

  // Eligibility and security flags
  eligibility: (staffNumber, query) => get(`/staff/${seg(staffNumber)}/eligibility`, query),
  securityFlags: (staffNumber) => get(`/staff/${seg(staffNumber)}/security-flags`),
  allSecurityFlags: (query) => get('/security-flags', query),
  setSecurityFlag: (staffNumber, flagType, body) => put(`/staff/${seg(staffNumber)}/security-flags/${seg(flagType)}`, body),

  // Payroll (StaffHr.Payroll role only)
  compensation: (staffNumber) => get(`/staff/${seg(staffNumber)}/compensation`),
  setCompensation: (staffNumber, body) => put(`/staff/${seg(staffNumber)}/compensation`, body),
  payrollHeadcount: () => get('/headcounts/payroll'),

  // Audit and health
  audit: (query) => get('/audit', query),
  health: () => get('/health'),
};

// ---- Display helpers ------------------------------------------------------------------------

/** "OnLeave" → "On leave", "HRReview" → "HR review", "AwaitingBudget" → "Awaiting budget". */
export function humanize(value) {
  if (value === undefined || value === null || value === '') return '—';
  const words = String(value)
    .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
    .replace(/([a-z\d])([A-Z])/g, '$1 $2')
    .split(' ');
  return words.map((w, i) => (i === 0 || /^[A-Z]{2,}$/.test(w) ? w : w.toLowerCase())).join(' ');
}

export const employmentStatuses = ['Active', 'Probation', 'OnLeave', 'Suspended', 'Terminated'];
export const employmentTypes = ['FullTime', 'PartTime', 'Contract', 'Seasonal'];
export const credentialStates = ['Valid', 'Expiring', 'Expired', 'Suspended'];
export const requisitionStatuses = ['Received', 'HRReview', 'AwaitingBudget', 'Approved', 'Rejected', 'Filling', 'Completed', 'Closed'];
export const openRequisitionStatuses = ['Received', 'HRReview', 'AwaitingBudget', 'Approved', 'Filling'];

const employmentTypeLabels = { FullTime: 'Full-time', PartTime: 'Part-time', Contract: 'Contract', Seasonal: 'Seasonal' };
export const employmentTypeLabel = (value) => employmentTypeLabels[value] ?? humanize(value);
export const requisitionStatusLabel = (value) => (value === 'AwaitingBudget' ? 'Awaiting Budget' : humanize(value));

/** Directory style name: "Achterberg, Desmond". */
export const sortName = (person) => (person ? `${person.lastName}, ${person.preferredName || person.firstName}` : '');

/** ISO date or date-time → "2026-11-05". */
export const isoDate = (value) => (value ? String(value).slice(0, 10) : '—');

/** Local date as "2026-11-05" (for <input type="date"> and API DateOnly values). */
export function localDate(date = new Date()) {
  const pad = (n) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/** Whole days from today to an ISO date (negative when it has passed). */
export function daysFromToday(value, today = new Date()) {
  if (!value) return null;
  const [y, m, d] = String(value).slice(0, 10).split('-').map(Number);
  const start = Date.UTC(today.getFullYear(), today.getMonth(), today.getDate());
  return Math.round((Date.UTC(y, m - 1, d) - start) / 86400000);
}

/** "12 days left", "Expired 3 days ago", "Today". */
export function remainingText(days) {
  if (days === null || days === undefined) return '—';
  if (days === 0) return 'Expires today';
  if (days < 0) return `Expired ${-days} day${days === -1 ? '' : 's'} ago`;
  return `${days} day${days === 1 ? '' : 's'} left`;
}

const money = new Intl.NumberFormat('en-CA', { style: 'currency', currency: 'CAD', maximumFractionDigits: 0 });
/** 1234567 → "$1,234,567". */
export const formatMoney = (value) => (value === null || value === undefined ? '—' : money.format(value));

const count = new Intl.NumberFormat('en-CA');
/** 1284 → "1,284". */
export const formatCount = (value) => (value === null || value === undefined ? '—' : count.format(value));

/** "2026-11-05T07:00:00Z" → "07:00" in local time. */
export const timeOfDay = (value) =>
  new Date(value).toLocaleTimeString('en-CA', { hour: '2-digit', minute: '2-digit', hour12: false });
