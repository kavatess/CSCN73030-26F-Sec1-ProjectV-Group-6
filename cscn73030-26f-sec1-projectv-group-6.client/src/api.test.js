import { afterEach, describe, expect, it, vi } from 'vitest';
import { api, ApiError, daysFromToday, employmentTypeLabel, humanize, remainingText, requisitionStatusLabel, sortName, toQuery } from './api.js';

const reply = (status, body) =>
  Promise.resolve(new Response(body === undefined ? null : JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } }));

afterEach(() => vi.unstubAllGlobals());

describe('api client', () => {
  it('leaves empty values out of the query string', () => {
    expect(toQuery({ q: 'des', department: '', page: 1, service: undefined })).toBe('?q=des&page=1');
    expect(toQuery({})).toBe('');
  });

  it('sends GET requests to the same origin and returns the JSON body', async () => {
    const fetch = vi.fn(() => reply(200, { items: [], total: 0 }));
    vi.stubGlobal('fetch', fetch);
    await expect(api.searchStaff({ department: 'EMS', status: 'Active' })).resolves.toEqual({ items: [], total: 0 });
    expect(fetch.mock.calls[0][0]).toBe('/staff?department=EMS&status=Active');
    expect(fetch.mock.calls[0][1].method).toBe('GET');
  });

  it('sends an Idempotency-Key with every new requisition', async () => {
    const fetch = vi.fn(() => reply(201, { requisitionNumber: 'REQ-0000007' }));
    vi.stubGlobal('fetch', fetch);
    await api.raiseRequisition({ type: 'Hire', departmentCode: 'EMS', roleTitle: 'Paramedic', positions: 1 }, 'key-1');
    const [url, init] = fetch.mock.calls[0];
    expect(url).toBe('/requisitions');
    expect(init.headers['Idempotency-Key']).toBe('key-1');
    expect(JSON.parse(init.body)).toMatchObject({ type: 'Hire', positions: 1 });
  });

  it('throws ProblemDetails responses as ApiError with the field errors', async () => {
    vi.stubGlobal('fetch', () => reply(422, { title: 'Validation failed', status: 422, errors: { email: ['Email is not valid.'] } }));
    const error = await api.createStaff({}).catch((e) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(422);
    expect(error.errors.email).toEqual(['Email is not valid.']);
    expect(error.message).toBe('Email is not valid.');
  });

  it('uses the ProblemDetails detail for conflicts', async () => {
    vi.stubGlobal('fetch', () => reply(409, { title: 'Conflict', status: 409, detail: "Email 'a@b.c' is already used by STF-0000003." }));
    await expect(api.createStaff({})).rejects.toThrow('already used by STF-0000003');
  });

  it('reports an unreachable server', async () => {
    vi.stubGlobal('fetch', () => Promise.reject(new TypeError('Failed to fetch')));
    const error = await api.health().catch((e) => e);
    expect(error.status).toBe(0);
    expect(error.message).toMatch(/could not be reached/);
  });

  it('returns null for 204 No Content', async () => {
    vi.stubGlobal('fetch', () => Promise.resolve(new Response(null, { status: 204 })));
    await expect(api.deleteRosterEntry(4)).resolves.toBeNull();
  });
});

describe('display helpers', () => {
  it('turns enum names into labels', () => {
    expect(humanize('OnLeave')).toBe('On leave');
    expect(humanize('HRReview')).toBe('HR review');
    expect(requisitionStatusLabel('AwaitingBudget')).toBe('Awaiting Budget');
    expect(employmentTypeLabel('FullTime')).toBe('Full-time');
  });

  it('formats names and remaining days', () => {
    expect(sortName({ firstName: 'Desmond', preferredName: 'Des', lastName: 'Achterberg' })).toBe('Achterberg, Des');
    const today = new Date(2026, 10, 5);
    expect(daysFromToday('2026-11-17', today)).toBe(12);
    expect(remainingText(12)).toBe('12 days left');
    expect(remainingText(-1)).toBe('Expired 1 day ago');
  });
});
