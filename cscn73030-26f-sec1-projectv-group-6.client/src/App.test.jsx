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

  it('shows the duplicate work email validation on onboarding', () => {
    render(<App />);
    fireEvent.click(screen.getByRole('link', { name:'Staff directory' }));
    fireEvent.click(screen.getByRole('button', { name:'Onboard employee' }));
    fireEvent.click(screen.getAllByRole('button', { name:'Create employee & issue ID' })[0]);
    expect(screen.getByText(/already held by STF-0003902/)).toBeTruthy();
  });
});
