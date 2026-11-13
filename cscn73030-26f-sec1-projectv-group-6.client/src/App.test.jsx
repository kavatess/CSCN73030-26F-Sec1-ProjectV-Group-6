import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import App from './App.jsx';
import { fakeApi } from './test/fakeApi.js';

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
    server = fakeApi({ 'GET /headcounts/payroll': { __reply:true, status:403, body:{ title:'Forbidden', status:403 } } });
    render(<App />);
    expect((await screen.findAllByText('Payroll role only')).length).toBe(7);
  });

  it('shows an error when the API cannot be reached', async () => {
    server = fakeApi({ 'GET /departments/headcount': () => { throw new TypeError('offline'); } });
    render(<App />);
    expect(await screen.findByRole('alert')).toBeTruthy();
  });

  it('filters the staff directory and opens the selected employee', () => {
    render(<App />);
    fireEvent.click(screen.getByRole('link', { name:'Staff directory' }));
    fireEvent.change(screen.getByRole('combobox', { name:'Department' }), { target:{ value:'Maintenance' } });
    fireEvent.click(screen.getByRole('button', { name:'Apply' }));
    expect(screen.getByRole('button', { name:'Mwangi, Tobias' })).toBeTruthy();
    expect(screen.queryByRole('button', { name:'Delacroix, Marcus' })).toBeNull();
    fireEvent.click(screen.getByRole('button', { name:'Mwangi, Tobias' }));
    expect(screen.getByRole('heading', { name:'Mwangi, Tobias' })).toBeTruthy();
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
