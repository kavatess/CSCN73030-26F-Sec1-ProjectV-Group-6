import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import App from './App.jsx';
import { fakeApi, problem, reply } from './test/fakeApi.js';

let server;
beforeEach(() => {
  window.history.replaceState(null, '', '/');
  server = fakeApi();
});
afterEach(cleanup);

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

  it('requires coverage before a draft roster can be published', () => {
    render(<App />);
    fireEvent.click(screen.getByRole('link', { name:'Duty roster' }));
    fireEvent.click(screen.getByRole('button', { name:'Publish roster' }));
    expect(screen.getByText(/Fill the two Saturday night slots first/)).toBeTruthy();
    fireEvent.click(screen.getAllByRole('button', { name:'Assign' })[0]);
    fireEvent.click(screen.getAllByRole('button', { name:'Assign' })[0]);
    fireEvent.click(screen.getByRole('button', { name:'Publish roster' }));
    expect(screen.getByText('Published')).toBeTruthy();
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
