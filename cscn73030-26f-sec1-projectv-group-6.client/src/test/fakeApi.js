/**
 * A stand-in for the Staff/HR API in the frontend tests. `fakeApi()` replaces global fetch with a
 * router over the fixtures below; pass overrides such as { 'POST /staff': problem(409, '...') }.
 * The fixtures are a small slice of the server's fictional seed data, in the API's JSON shapes.
 */
import { vi } from 'vitest';

const dept = (code) => ({ code, name: departments.find((d) => d.code === code).name });

export const departments = [
  { code: 'BUD', name: 'Budget & Treasury', serviceCount: 3 },
  { code: 'EMS', name: 'Emergency Services', serviceCount: 3 },
  { code: 'HRS', name: 'Human Resources', serviceCount: 3 },
  { code: 'MNT', name: 'Maintenance', serviceCount: 3 },
  { code: 'PTR', name: 'Public Transport', serviceCount: 3 },
  { code: 'RES', name: 'Residence', serviceCount: 3 },
  { code: 'SEC', name: 'Security & Policing', serviceCount: 3 },
];

export const services = {
  EMS: [
    { serviceId: 3, name: 'Emergency Dispatch', departmentCode: 'EMS' },
    { serviceId: 2, name: 'Fire & Rescue', departmentCode: 'EMS' },
    { serviceId: 1, name: 'Paramedic Service', departmentCode: 'EMS' },
  ],
  MNT: [
    { serviceId: 11, name: 'Roads & Sidewalks', departmentCode: 'MNT' },
    { serviceId: 10, name: 'Water & Sewer', departmentCode: 'MNT' },
  ],
};

export const headcount = [
  { code: 'BUD', name: 'Budget & Treasury', active: 1, onDuty: 0, vacancies: 0 },
  { code: 'EMS', name: 'Emergency Services', active: 7, onDuty: 2, vacancies: 2 },
  { code: 'HRS', name: 'Human Resources', active: 2, onDuty: 0, vacancies: 0 },
  { code: 'MNT', name: 'Maintenance', active: 2, onDuty: 0, vacancies: 0 },
  { code: 'PTR', name: 'Public Transport', active: 2, onDuty: 0, vacancies: 0 },
  { code: 'RES', name: 'Residence', active: 1, onDuty: 0, vacancies: 0 },
  { code: 'SEC', name: 'Security & Policing', active: 2, onDuty: 0, vacancies: 2 },
];

export const payroll = {
  asOf: '2026-11-05',
  departments: headcount.map((d) => ({
    code: d.code, name: d.name, headcount: d.active, fullTime: d.active, partTime: 0,
    totalAnnualSalary: d.active * 75000, averageAnnualSalary: 75000, missingCompensation: 0,
  })),
  totalHeadcount: 17,
  totalAnnualSalary: 1275000,
};

const summary = (staffNumber, firstName, lastName, status, code, service, role, credentialState) => ({
  staffNumber, displayName: `${firstName} ${lastName}`, firstName, lastName,
  email: `${firstName}.${lastName}@cmes.example`.toLowerCase(), phone: '555-0100',
  employmentStatus: status, employmentType: 'FullTime', homeDepartment: dept(code),
  currentRole: role, currentService: service, credentialState,
});

export const staff = [
  summary('STF-0000011', 'Noor', 'Abernathy', 'Suspended', 'SEC', 'Bylaw Enforcement', 'Bylaw Officer', 'Suspended'),
  { ...summary('STF-0000002', 'Desmond', 'Achterberg', 'Active', 'EMS', 'Paramedic Service', 'Advanced Care Paramedic', 'Expiring'), displayName: 'Des Achterberg' },
  summary('STF-0000001', 'Talia', 'Brennwood', 'Active', 'EMS', 'Paramedic Service', 'Paramedic Supervisor', 'Valid'),
  summary('STF-0000015', 'Saoirse', 'Delacroix', 'Active', 'MNT', 'Water & Sewer', 'Water Systems Operator', 'Expired'),
  summary('STF-0000005', 'Ines', 'Halvorsen', 'Probation', 'EMS', 'Paramedic Service', 'Primary Care Paramedic', 'Valid'),
  summary('STF-0000016', 'Bram', 'Oyelaran', 'Active', 'MNT', 'Roads & Sidewalks', 'Road Crew Lead', 'Valid'),
];

export const credentials = [
  { credentialId: 4, staffNumber: 'STF-0000002', staffName: 'Des Achterberg', departmentCode: 'EMS', credentialTypeId: 3, credentialType: 'CPR & First Aid Certificate', isDutyBlocking: true, number: 'FAKE-1003', issueDate: '2024-11-24', expiryDate: '2026-11-24', status: 'Expiring', daysUntilExpiry: 12 },
  { credentialId: 3, staffNumber: 'STF-0000002', staffName: 'Des Achterberg', departmentCode: 'EMS', credentialTypeId: 2, credentialType: 'Advanced Care Paramedic Licence', isDutyBlocking: true, number: 'FAKE-1002', issueDate: '2025-06-01', expiryDate: '2027-06-01', status: 'Valid', daysUntilExpiry: 201 },
  { credentialId: 18, staffNumber: 'STF-0000015', staffName: 'Saoirse Delacroix', departmentCode: 'MNT', credentialTypeId: 10, credentialType: 'Confined Space Entry Training', isDutyBlocking: false, number: 'FAKE-1018', issueDate: '2024-10-01', expiryDate: '2026-10-01', status: 'Expired', daysUntilExpiry: -42 },
  { credentialId: 17, staffNumber: 'STF-0000011', staffName: 'Noor Abernathy', departmentCode: 'SEC', credentialTypeId: 3, credentialType: 'CPR & First Aid Certificate', isDutyBlocking: true, number: 'FAKE-1017', issueDate: '2025-02-01', expiryDate: '2027-02-01', status: 'Suspended', daysUntilExpiry: 81 },
];

export const credentialTypes = [
  { credentialTypeId: 2, name: 'Advanced Care Paramedic Licence', isDutyBlocking: true },
  { credentialTypeId: 3, name: 'CPR & First Aid Certificate', isDutyBlocking: true },
  { credentialTypeId: 10, name: 'Confined Space Entry Training', isDutyBlocking: false },
];

export const requisitions = [
  { requisitionNumber: 'REQ-0000004', type: 'Hire', department: dept('PTR'), roleTitle: 'Bus Operator', positions: 4, positionsFilled: 0, status: 'AwaitingBudget', reasonCode: 'SERVICE_EXPANSION', budgetDecision: 'Pending', staffNumber: null, createdAtUtc: '2026-10-22T12:00:00Z' },
  { requisitionNumber: 'REQ-0000002', type: 'Hire', department: dept('EMS'), roleTitle: 'Primary Care Paramedic', positions: 2, positionsFilled: 1, status: 'Filling', reasonCode: 'VACANCY', budgetDecision: 'Approved', staffNumber: null, createdAtUtc: '2026-09-30T12:00:00Z' },
  { requisitionNumber: 'REQ-0000001', type: 'Hire', department: dept('SEC'), roleTitle: 'Patrol Constable', positions: 2, positionsFilled: 0, status: 'Rejected', reasonCode: 'GROWTH', budgetDecision: 'Rejected', staffNumber: null, createdAtUtc: '2026-09-01T12:00:00Z' },
];

export const securityFlags = [
  { flagId: 1, staffNumber: 'STF-0000011', flagType: 'UnderInvestigation', status: 'Active', updatedAt: '2026-10-31T12:00:00Z' },
];

export const profile = {
  ...staff[1], preferredName: 'Des', dateOfBirth: '1987-02-04', citizenId: 'CIT-FAKE-001037', hireDate: '2018-11-12',
  assignments: [
    { assignmentId: 2, staffNumber: 'STF-0000002', serviceId: 1, serviceName: 'Paramedic Service', departmentCode: 'EMS', roleTitle: 'Advanced Care Paramedic', startDate: '2024-11-12', endDate: null, isCurrent: true },
    { assignmentId: 22, staffNumber: 'STF-0000002', serviceId: 1, serviceName: 'Paramedic Service', departmentCode: 'EMS', roleTitle: 'Primary Care Paramedic', startDate: '2018-11-12', endDate: '2024-11-11', isCurrent: false },
  ],
  credentials: credentials.filter((c) => c.staffNumber === 'STF-0000002'),
};

export const audit = {
  items: [
    { auditId: 9, entityType: 'Staff', entityId: 'STF-0000002', action: 'Updated', actorId: 'dev-user', timestampUtc: '2026-11-05T11:00:00Z' },
    { auditId: 8, entityType: 'Requisition', entityId: 'REQ-0000004', action: 'Created', actorId: 'system', timestampUtc: '2026-11-05T10:00:00Z' },
  ],
  page: 1, pageSize: 50, total: 2,
};

/** A route reply with a status code other than 200. */
export const reply = (status, body) => ({ __reply: true, status, body });
/** An RFC 7807 error reply. */
export const problem = (status, detail, errors) =>
  reply(status, { type: 'about:blank', title: detail, status, detail, ...(errors ? { errors } : {}) });

const page = (items, query) => ({ items, page: Number(query.page || 1), pageSize: Number(query.pageSize || 20), total: items.length });

function defaultRoutes() {
  return {
    'GET /health': { status: 'Healthy', checks: { database: 'Healthy', outbox: 'Healthy' } },
    'GET /departments': departments,
    'GET /departments/headcount': headcount,
    'GET /departments/:code/services': ({ params }) => services[params[0]] ?? [],
    'GET /headcounts/payroll': payroll,
    'GET /staff': ({ query }) => page(staff.filter((s) =>
      (!query.department || s.homeDepartment.code === query.department) &&
      (!query.status || s.employmentStatus === query.status) &&
      (!query.credentialState || s.credentialState === query.credentialState) &&
      (!query.q || `${s.firstName} ${s.lastName} ${s.staffNumber} ${s.email}`.toLowerCase().includes(query.q.toLowerCase()))), query),
    'GET /staff/:n': ({ params }) => (params[0] === profile.staffNumber ? profile
      : staff.some((s) => s.staffNumber === params[0]) ? { ...profile, preferredName: null, ...staff.find((s) => s.staffNumber === params[0]), assignments: [], credentials: [] }
        : problem(404, `Staff '${params[0]}' was not found.`)),
    'GET /staff/:n/eligibility': ({ params }) => ({ staffNumber: params[0], eligible: true, at: '2026-11-05T12:00:00Z', serviceId: null, reasons: [], warnings: [] }),
    'GET /staff/:n/security-flags': ({ params }) => securityFlags.filter((f) => f.staffNumber === params[0]),
    'GET /staff/:n/compensation': [],
    'GET /security-flags': securityFlags,
    'GET /credentials': ({ query }) => page(credentials.filter((c) => !query.state || c.status === query.state), query),
    'GET /credentials/types': credentialTypes,
    'GET /credentials/expiring': ({ query }) => credentials.filter((c) => c.daysUntilExpiry >= 0 && c.daysUntilExpiry <= Number(query.withinDays) && c.status !== 'Suspended'),
    'GET /requisitions': ({ query }) => requisitions.filter((r) => !query.status || r.status === query.status),
    'GET /rosters': [],
    'GET /rosters/headcount': [],
    'GET /audit': audit,
  };
}

/** Replaces fetch with the fake API. Returns the list of calls made, for assertions. */
export function fakeApi(overrides = {}) {
  const routes = { ...defaultRoutes(), ...overrides };
  const calls = [];
  const fetch = vi.fn(async (input, init = {}) => {
    const url = new URL(input, 'http://localhost');
    const method = init.method || 'GET';
    const query = Object.fromEntries(url.searchParams);
    const body = init.body ? JSON.parse(init.body) : undefined;
    calls.push({ method, path: url.pathname, query, body, headers: init.headers || {} });
    for (const [route, handler] of Object.entries(routes)) {
      const [verb, pattern] = route.split(' ');
      const match = verb === method && url.pathname.match(new RegExp('^' + pattern.replace(/:\w+/g, '([^/]+)') + '$'));
      if (!match) continue;
      const result = typeof handler === 'function' ? handler({ params: match.slice(1).map(decodeURIComponent), query, body }) : handler;
      const { status, body: out } = result?.__reply ? result : { status: 200, body: result };
      return new Response(out === undefined || status === 204 ? null : JSON.stringify(out), { status, headers: { 'Content-Type': 'application/json' } });
    }
    return new Response(JSON.stringify({ title: 'Not Found', status: 404, detail: `No fake route for ${method} ${url.pathname}` }), { status: 404 });
  });
  vi.stubGlobal('fetch', fetch);
  return { calls, fetch };
}
