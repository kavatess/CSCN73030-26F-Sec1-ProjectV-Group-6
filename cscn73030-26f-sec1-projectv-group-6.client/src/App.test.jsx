import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import App from './App.jsx';
import { credentials, fakeApi, problem, reply, requisitions } from './test/fakeApi.js';

let server;
beforeEach(() => {
  window.history.replaceState(null, '', '/');
  server = fakeApi();
});
afterEach(() => { cleanup(); vi.restoreAllMocks(); });

describe('CMES Staff & HR frontend', () => {
  it('renders the workforce overview from the live headcount and payroll endpoints', async () => {
    render(<App />);
    expect(screen.getByRole('heading', { name:'Workforce overview' })).toBeTruthy();
    const table = screen.getByRole('table');
    expect(await within(table).findByRole('button', { name:'Emergency Services' })).toBeTruthy();
    expect(within(table).getAllByRole('row')).toHaveLength(8);
    expect(within(table).getByText('$525,000')).toBeTruthy();
    expect(await screen.findByText('1 awaiting Budget')).toBeTruthy();
    expect(screen.getByText(/UnderInvestigation|Under investigation/)).toBeTruthy();
    expect(await screen.findByText('Connected')).toBeTruthy();
    expect(server.calls.map(c => c.path)).toEqual(expect.arrayContaining(['/departments/headcount', '/headcounts/payroll', '/credentials/expiring']));
  });

  it('says when the payroll totals need the Payroll role', async () => {
    server = fakeApi({ 'GET /headcounts/payroll': problem(403, 'Forbidden') });
    render(<App />);
    expect((await screen.findAllByText('Payroll role only')).length).toBe(7);
  });

  it('shows an error when the API cannot be reached', async () => {
    server = fakeApi({ 'GET /departments/headcount': () => { throw new TypeError('offline'); } });
    render(<App />);
    expect(await screen.findByRole('alert')).toBeTruthy();
  });

  it('searches the directory on the server and opens the selected employee', async () => {
    render(<App />);
    fireEvent.click(screen.getByRole('link', { name:'Staff directory' }));
    expect(await screen.findByRole('button', { name:'Achterberg, Desmond' })).toBeTruthy();
    await screen.findByRole('option', { name:'Maintenance' });
    fireEvent.change(screen.getByRole('combobox', { name:'Department' }), { target:{ value:'MNT' } });
    fireEvent.click(screen.getByRole('button', { name:'Apply' }));
    expect(await screen.findByRole('button', { name:'Delacroix, Saoirse' })).toBeTruthy();
    await waitFor(() => expect(screen.queryByRole('button', { name:'Achterberg, Desmond' })).toBeNull());
    expect(server.calls).toContainEqual(expect.objectContaining({ path:'/staff', query:expect.objectContaining({ department:'MNT', page:'1', pageSize:'20' }) }));
    fireEvent.click(screen.getByRole('button', { name:'Delacroix, Saoirse' }));
    expect(await screen.findByRole('heading', { name:'Delacroix, Saoirse' })).toBeTruthy();
    expect(location.hash).toBe('#profile/STF-0000015');
  });

  it('filters by employment status with live counts', async () => {
    render(<App />);
    fireEvent.click(screen.getByRole('link', { name:'Staff directory' }));
    const probation = await screen.findByRole('button', { name:'Probation 1' });
    fireEvent.click(probation);
    expect(await screen.findByText('Probation staff — 1 results')).toBeTruthy();
    expect(screen.getByRole('button', { name:'Halvorsen, Ines' })).toBeTruthy();
  });

  it('shows the profile, eligibility and assignment history from the API', async () => {
    window.location.hash = '#profile/STF-0000002';
    render(<App />);
    expect(await screen.findByRole('heading', { name:'Achterberg, Des' })).toBeTruthy();
    expect(screen.getByText('CIT-FAKE-001037')).toBeTruthy();
    expect(await screen.findByText('Eligible for duty')).toBeTruthy();
    expect(screen.getByText('Clear')).toBeTruthy();
    fireEvent.click(screen.getByRole('tab', { name:'Assignments' }));
    expect(screen.getByText('Primary Care Paramedic')).toBeTruthy();
    expect(screen.getByRole('button', { name:'End today' })).toBeTruthy();
  });

  it('saves profile edits and shows field errors from a 422', async () => {
    server = fakeApi({ 'PATCH /staff/:n': ({ body }) => body.email.endsWith('@cmes.example') ? { ...body } : problem(422, 'One or more fields are invalid.', { email:['Email must be a valid address.'] }) });
    window.location.hash = '#profile/STF-0000002';
    render(<App />);
    fireEvent.click(await screen.findByRole('button', { name:'Edit profile' }));
    const dialog = screen.getByRole('form', { name:'Edit profile' });
    fireEvent.change(within(dialog).getByLabelText('Work email'), { target:{ value:'des@elsewhere' } });
    fireEvent.click(within(dialog).getByRole('button', { name:'Save changes' }));
    expect(await within(dialog).findByText('Email must be a valid address.')).toBeTruthy();
    fireEvent.change(within(dialog).getByDisplayValue('des@elsewhere'), { target:{ value:'des@cmes.example' } });
    fireEvent.click(within(dialog).getByRole('button', { name:'Save changes' }));
    expect(await screen.findByText('Profile saved.')).toBeTruthy();
    expect(server.calls.filter(c => c.method === 'PATCH').at(-1).body).toMatchObject({ email:'des@cmes.example', preferredName:'Des' });
  });

  it('raises a termination requisition with an idempotency key', async () => {
    server = fakeApi({ 'POST /requisitions': reply(201, { requisitionNumber:'REQ-0000007', type:'Termination', status:'Received' }) });
    window.location.hash = '#profile/STF-0000002';
    render(<App />);
    fireEvent.click(await screen.findByRole('button', { name:'Request termination' }));
    fireEvent.change(screen.getByRole('combobox', { name:'Termination reason' }), { target:{ value:'RETIREMENT' } });
    fireEvent.click(screen.getByRole('button', { name:'Raise termination requisition' }));
    expect(await screen.findByText(/REQ-0000007 raised/)).toBeTruthy();
    const call = server.calls.find(c => c.method === 'POST');
    expect(call.body).toEqual({ type:'Termination', staffNumber:'STF-0000002', departmentCode:'EMS', reasonCode:'RETIREMENT' });
    expect(call.headers['Idempotency-Key']).toBeTruthy();
  });

  it('keeps salary behind the Payroll role', async () => {
    server = fakeApi({ 'GET /staff/:n/compensation': problem(403, 'Forbidden') });
    window.location.hash = '#profile/STF-0000002';
    render(<App />);
    fireEvent.click(await screen.findByRole('tab', { name:'Compensation' }));
    expect(await screen.findByText(/Individual salary belongs to HR Payroll/)).toBeTruthy();
  });

  it('shows this week\'s roster with eligibility and adds a shift after checking it', async () => {
    const today = new Date(); today.setHours(7, 0, 0, 0);
    const shift = { rosterEntryId:5, staffNumber:'STF-0000001', staffName:'Talia Brennwood', serviceId:1, serviceName:'Paramedic Service', shiftStart:today.toISOString(), shiftEnd:new Date(today.getTime() + 12 * 3600000).toISOString(), shiftType:'Day', status:'Scheduled' };
    server = fakeApi({
      'GET /rosters': [shift],
      'GET /staff/:n/eligibility': ({ params, query }) => params[0] === 'STF-0000005'
        ? { staffNumber:params[0], eligible:false, serviceId:Number(query.service), reasons:[{ code:'CREDENTIAL_EXPIRED', message:'ACP licence expired.' }], warnings:[] }
        : { staffNumber:params[0], eligible:true, serviceId:Number(query.service), reasons:[], warnings:[] },
      'POST /rosters/entries': ({ body }) => body.staffNumber === 'STF-0000001'
        ? problem(409, 'STF-0000001 is already rostered, which overlaps this shift.')
        : reply(201, { rosterEntryId:6, ...body }),
    });
    window.location.hash = '#roster';
    render(<App />);
    expect(await screen.findByRole('button', { name:'BRENNWOOD, TALIA' })).toBeTruthy();
    expect(screen.getByTitle(/Day .* · Scheduled/)).toBeTruthy();
    expect(await screen.findByText('ACP licence expired.')).toBeTruthy();
    expect(server.calls).toContainEqual(expect.objectContaining({ path:'/rosters', query:expect.objectContaining({ service:'1' }) }));

    const candidates = screen.getAllByRole('button', { name:'Assign' });
    fireEvent.click(candidates[0]);
    const dialog = screen.getByRole('form', { name:'Add shift · Paramedic Service' });
    fireEvent.change(within(dialog).getByRole('combobox', { name:'Employee' }), { target:{ value:'STF-0000001' } });
    fireEvent.click(within(dialog).getByRole('button', { name:'Add shift' }));
    expect(await within(dialog).findByText(/overlaps this shift/)).toBeTruthy();
    fireEvent.change(within(dialog).getByRole('combobox', { name:'Employee' }), { target:{ value:'STF-0000002' } });
    fireEvent.change(within(dialog).getByRole('combobox', { name:'Shift type' }), { target:{ value:'Night' } });
    fireEvent.click(within(dialog).getByRole('button', { name:'Add shift' }));
    expect(await screen.findByText(/Night shift added for STF-0000002/)).toBeTruthy();
    const post = server.calls.filter(c => c.method === 'POST').at(-1);
    expect(post.body).toMatchObject({ staffNumber:'STF-0000002', serviceId:1, shiftType:'Night' });
    expect((new Date(post.body.shiftEnd) - new Date(post.body.shiftStart)) / 3600000).toBe(12);
    expect(server.calls).toContainEqual(expect.objectContaining({ path:'/staff/STF-0000002/eligibility', query:expect.objectContaining({ service:'1' }) }));
  });

  it('refuses to roster someone who is not eligible', async () => {
    server = fakeApi({ 'GET /staff/:n/eligibility': ({ params }) => ({ staffNumber:params[0], eligible:false, reasons:[{ code:'EMPLOYMENT_STATUS', message:'Employment status is OnLeave.' }], warnings:[] }) });
    window.location.hash = '#roster';
    render(<App />);
    await screen.findByRole('button', { name:'BRENNWOOD, TALIA' });
    fireEvent.click(screen.getByRole('button', { name:'Add shift' }));
    const dialog = screen.getByRole('form', { name:'Add shift · Paramedic Service' });
    fireEvent.change(within(dialog).getByRole('combobox', { name:'Employee' }), { target:{ value:'STF-0000001' } });
    fireEvent.click(within(dialog).getByRole('button', { name:'Add shift' }));
    expect(await within(dialog).findByText(/Not eligible for duty: Employment status is OnLeave/)).toBeTruthy();
    expect(server.calls.some(c => c.method === 'POST')).toBe(false);
  });

  it('lists requisitions and moves one through the workflow', async () => {
    server = fakeApi({ 'PATCH /requisitions/:n': ({ params, body }) => ({ ...requisitions.find(r => r.requisitionNumber === params[0]), ...body }) });
    render(<App />);
    fireEvent.click(screen.getByRole('link', { name:/Requisitions/ }));
    expect((await screen.findAllByText('Bus Operator')).length).toBeGreaterThan(0);
    expect(screen.queryByText('Patrol Constable')).toBeNull(); // rejected, so not open
    expect(screen.getByText('Oldest waiting', { exact:false })).toBeTruthy();
    fireEvent.click(screen.getByRole('button', { name:'Budget approved' }));
    await waitFor(() => expect(server.calls.some(c => c.method === 'PATCH')).toBe(true));
    expect(server.calls.find(c => c.method === 'PATCH')).toMatchObject({ path:'/requisitions/REQ-0000004', body:{ budgetDecision:'Approved', status:'Approved' } });
    fireEvent.change(screen.getByRole('combobox', { name:'Filter by status' }), { target:{ value:'' } });
    expect((await screen.findAllByText('Patrol Constable')).length).toBeGreaterThan(0);
  });

  it('raises a hire requisition and reuses its idempotency key on retry', async () => {
    let attempts = 0;
    server = fakeApi({ 'POST /requisitions': ({ body }) => (++attempts === 1 ? problem(503, 'Service unavailable') : reply(201, { ...body, requisitionNumber:'REQ-0000007', status:'Received', department:{ code:'EMS', name:'Emergency Services' }, positionsFilled:0, createdAtUtc:'2026-11-05T12:00:00Z' })) });
    window.location.hash = '#requisitions';
    render(<App />);
    await screen.findAllByText('Bus Operator');
    fireEvent.click(screen.getByRole('button', { name:'Raise requisition' }));
    const dialog = screen.getByRole('form', { name:'Raise requisition' });
    fireEvent.change(within(dialog).getByRole('combobox', { name:'Requesting department' }), { target:{ value:'EMS' } });
    fireEvent.change(within(dialog).getByLabelText('Role'), { target:{ value:'Paramedic' } });
    fireEvent.change(within(dialog).getByLabelText('Positions'), { target:{ value:'3' } });
    fireEvent.click(within(dialog).getByRole('button', { name:'Raise requisition' }));
    expect(await within(dialog).findByText('Service unavailable')).toBeTruthy();
    fireEvent.click(within(dialog).getByRole('button', { name:'Raise requisition' }));
    expect(await screen.findByText('Requisition REQ-0000007 raised.')).toBeTruthy();
    const posts = server.calls.filter(c => c.method === 'POST');
    expect(posts).toHaveLength(2);
    expect(posts[0].headers['Idempotency-Key']).toBe(posts[1].headers['Idempotency-Key']);
    expect(posts[1].body).toEqual({ type:'Hire', departmentCode:'EMS', roleTitle:'Paramedic', positions:3, reasonCode:'VACANCY' });
  });

  it('tracks credentials by state and records a renewal', async () => {
    server = fakeApi({ 'PATCH /credentials/:id': ({ params, body }) => ({ ...credentials.find(c => String(c.credentialId) === params[0]), ...body, status:'Valid', daysUntilExpiry:700 }) });
    window.location.hash = '#credentials';
    render(<App />);
    expect(await screen.findByRole('button', { name:'Expired 1' })).toBeTruthy();
    const blocking = screen.getByRole('heading', { name:'Blocking duty right now' }).closest('section');
    expect(within(blocking).getByText('Noor Abernathy')).toBeTruthy();
    fireEvent.click(screen.getByRole('button', { name:'Expired 1' }));
    expect(await screen.findByText('Expired credentials — 1 records')).toBeTruthy();
    expect(server.calls).toContainEqual(expect.objectContaining({ path:'/credentials', query:expect.objectContaining({ state:'Expired' }) }));
    fireEvent.click(screen.getByRole('button', { name:'Renew' }));
    const dialog = screen.getByRole('form', { name:'Record renewal · Saoirse Delacroix' });
    fireEvent.change(within(dialog).getByLabelText('New expiry'), { target:{ value:'2028-10-01' } });
    fireEvent.click(within(dialog).getByRole('button', { name:'Record renewal' }));
    expect(await screen.findByText(/renewed — now valid/)).toBeTruthy();
    expect(server.calls.find(c => c.method === 'PATCH')).toMatchObject({ path:'/credentials/18', body:{ number:'FAKE-1018', expiryDate:'2028-10-01' } });
  });

  it('suspends a credential after confirmation', async () => {
    server = fakeApi({ 'PATCH /credentials/:id': ({ body }) => ({ ...credentials[0], ...body, status:'Suspended' }) });
    vi.spyOn(window, 'confirm').mockReturnValue(true);
    window.location.hash = '#credentials';
    render(<App />);
    await screen.findByText('FAKE-1003');
    fireEvent.click(within(screen.getByText('FAKE-1003').closest('tr')).getByRole('button', { name:'Suspend' }));
    await waitFor(() => expect(server.calls.find(c => c.method === 'PATCH')).toMatchObject({ path:'/credentials/4', body:{ suspended:true } }));
    expect(window.confirm).toHaveBeenCalledWith(expect.stringMatching(/fail the duty eligibility check/));
  });

  it('onboards an employee, records credentials and fills the requisition', async () => {
    server = fakeApi({
      'GET /requisitions': [{ requisitionNumber:'REQ-0000002', type:'Hire', department:{ code:'EMS', name:'Emergency Services' }, roleTitle:'Primary Care Paramedic', positions:2, positionsFilled:1, status:'Filling', budgetDecision:'Approved', createdAtUtc:'2026-09-30T12:00:00Z' }],
      'POST /staff': ({ body }) => reply(201, { ...body, staffNumber:'STF-0000021' }),
      'POST /staff/:n/credentials': ({ body }) => reply(201, { credentialId:99, ...body }),
      'PATCH /requisitions/:n': ({ body }) => ({ requisitionNumber:'REQ-0000002', ...body }),
    });
    window.location.hash = '#onboard';
    render(<App />);
    await screen.findByRole('option', { name:/REQ-0000002/ });
    fireEvent.change(screen.getByRole('combobox', { name:'Fills requisition' }), { target:{ value:'REQ-0000002' } });
    expect(screen.getByText(/Filling position 2 of 2/)).toBeTruthy();
    fireEvent.change(screen.getByLabelText('Legal first name'), { target:{ value:'Sofia' } });
    fireEvent.change(screen.getByLabelText('Legal last name'), { target:{ value:'Marchetti' } });
    fireEvent.change(screen.getByLabelText('Date of birth'), { target:{ value:'1996-08-12' } });
    fireEvent.change(screen.getByLabelText('Work email'), { target:{ value:'sofia.marchetti@cmes.example' } });
    await screen.findByRole('option', { name:'Paramedic Service' });
    fireEvent.change(screen.getByRole('combobox', { name:'Service' }), { target:{ value:'1' } });
    await screen.findByRole('option', { name:'CPR & First Aid Certificate' });
    fireEvent.change(screen.getByRole('combobox', { name:'Credential type 1' }), { target:{ value:'3' } });
    fireEvent.change(screen.getByLabelText('Credential number 1'), { target:{ value:'CPR-1' } });
    fireEvent.change(screen.getByLabelText('Issued date 1'), { target:{ value:'2026-01-01' } });
    fireEvent.change(screen.getByLabelText('Expiry date 1'), { target:{ value:'2028-01-01' } });
    fireEvent.click(screen.getAllByRole('button', { name:'Create employee & issue ID' })[0]);
    expect(await screen.findByText('STF-0000021')).toBeTruthy();
    const post = server.calls.find(c => c.method === 'POST' && c.path === '/staff');
    expect(post.body).toMatchObject({ firstName:'Sofia', homeDepartmentCode:'EMS', employmentStatus:'Probation', initialAssignment:{ serviceId:1, roleTitle:'Primary Care Paramedic' } });
    expect(server.calls).toContainEqual(expect.objectContaining({ method:'POST', path:'/staff/STF-0000021/credentials', body:expect.objectContaining({ credentialTypeId:3, number:'CPR-1' }) }));
    expect(server.calls.filter(c => c.method === 'PATCH').map(c => c.body)).toEqual([{ positionsFilled:2 }, { status:'Completed' }]);
    expect(await screen.findByText('REQ-0000002: 2 of 2 positions filled')).toBeTruthy();
  });

  it('shows the 409 for a work email that is already in use', async () => {
    server = fakeApi({ 'POST /staff': problem(409, "Email 'des@cmes.example' is already used by STF-0000002.") });
    window.location.hash = '#onboard';
    render(<App />);
    await screen.findByRole('option', { name:'Emergency Services' });
    fireEvent.change(screen.getByLabelText('Legal first name'), { target:{ value:'Des' } });
    fireEvent.change(screen.getByLabelText('Legal last name'), { target:{ value:'Again' } });
    fireEvent.change(screen.getByLabelText('Date of birth'), { target:{ value:'1990-01-01' } });
    fireEvent.change(screen.getByLabelText('Work email'), { target:{ value:'des@cmes.example' } });
    fireEvent.change(screen.getByRole('combobox', { name:'Home department' }), { target:{ value:'EMS' } });
    fireEvent.click(screen.getAllByRole('button', { name:'Create employee & issue ID' })[0]);
    expect(await screen.findByText(/already used by STF-0000002/)).toBeTruthy();
  });

  it('asks for the required fields before calling the API', async () => {
    window.location.hash = '#onboard';
    render(<App />);
    fireEvent.click(screen.getAllByRole('button', { name:'Create employee & issue ID' })[0]);
    expect(screen.getByText('Legal first name is required.')).toBeTruthy();
    expect(server.calls.some(c => c.method === 'POST')).toBe(false);
  });
});
