import { useState } from 'react';
import { api, daysFromToday, formatCount, formatMoney, humanize, openRequisitionStatuses, timeOfDay, isoDate } from '../api';
import { useApi } from '../useApi';
import { Pill, Button, Stat, Panel, ApiNotice, Empty } from '../ui';
import { csvDownload, routes } from '../screens';

/** Budget's SLA for a funding decision, in days. */
const BUDGET_SLA_DAYS = 5;

/** The "Requires attention" list, built from live credential, flag, requisition and vacancy data. */
function attentionItems({ expired, expiring, flags, requisitions, headcount }) {
  const items = [];
  const blockingExpired = expired.filter((c) => c.isDutyBlocking);
  if (blockingExpired.length) items.push({ tag: 'Blocks duty', code: 'CRED', kind: 'bad', copy: `${blockingExpired.length} duty-blocking credential${blockingExpired.length === 1 ? ' has' : 's have'} expired — those employees fail the eligibility check`, link: 'Open credential tracker →', to: ['credentials', { state: 'Expired' }] });
  const soon = expiring.filter((c) => c.isDutyBlocking && c.daysUntilExpiry <= 14);
  if (soon.length) items.push({ tag: 'Expiring', code: 'CRED', kind: 'warn', copy: `${soon.length} duty-blocking credential${soon.length === 1 ? '' : 's'} expire within 14 days`, link: 'Open credential tracker →', to: ['credentials', { state: 'Expiring' }] });
  for (const flag of flags) items.push({ tag: 'Security flag', code: 'SEC', kind: 'bad', copy: `${humanize(flag.flagType)} is active for ${flag.staffNumber} — not eligible for duty`, link: 'Review profile →', to: ['profile', { staffNumber: flag.staffNumber }] });
  for (const r of requisitions.filter((x) => x.status === 'AwaitingBudget')) {
    const waited = -daysFromToday(r.createdAtUtc);
    if (waited > BUDGET_SLA_DAYS) items.push({ tag: 'Stalled', code: r.department.code, kind: 'warn', copy: `${r.requisitionNumber} has waited ${waited} days on a Budget decision`, link: 'Open requisition →', to: ['requisitions', { selected: r.requisitionNumber }] });
  }
  for (const d of headcount.filter((x) => x.vacancies > 0)) items.push({ tag: 'Vacancies', code: d.code, kind: 'warn', copy: `${d.name} has ${d.vacancies} approved position${d.vacancies === 1 ? '' : 's'} still to fill`, link: 'Open requisitions →', to: ['requisitions', {}] });
  return items;
}

export default function Overview({ go, notify }) {
  const [search, setSearch] = useState('');
  const headcount = useApi(() => api.departmentHeadcount(), []);
  const payroll = useApi(() => api.payrollHeadcount(), []);
  const requisitions = useApi(() => api.requisitions(), []);
  const expiring = useApi(() => api.expiringCredentials(30), []);
  const expired = useApi(() => api.credentials({ state: 'Expired', pageSize: 100 }), []);
  const flags = useApi(() => api.allSecurityFlags({ status: 'Active' }), []);
  const audit = useApi(() => api.audit({ pageSize: 6 }), []);

  const departments = headcount.data ?? [];
  const sum = (key) => departments.reduce((total, d) => total + d[key], 0);
  const onDutyDepartments = departments.filter((d) => d.onDuty > 0).length;
  const open = (requisitions.data ?? []).filter((r) => openRequisitionStatuses.includes(r.status));
  const awaitingBudget = open.filter((r) => r.status === 'AwaitingBudget').length;
  const expiringRows = expiring.data ?? [];
  const blocking = expiringRows.filter((c) => c.isDutyBlocking).length;
  const payrollByCode = Object.fromEntries((payroll.data?.departments ?? []).map((d) => [d.code, d]));
  const payrollCell = (code) => payroll.data ? formatMoney(payrollByCode[code]?.totalAnnualSalary ?? 0) : payroll.error?.status === 403 ? 'Payroll role only' : '…';
  const maxActive = Math.max(1, ...departments.map((d) => d.active));
  const attention = attentionItems({ expired: expired.data?.items ?? [], expiring: expiringRows, flags: flags.data ?? [], requisitions: requisitions.data ?? [], headcount: departments });

  const exportCsv = () => {
    csvDownload('cmes-headcount.csv', ['Department', 'Code', 'Active', 'On duty', 'Vacancies', 'Annual payroll'],
      departments.map((d) => [d.name, d.code, d.active, d.onDuty, d.vacancies, payrollByCode[d.code]?.totalAnnualSalary ?? '']));
    notify('Headcount CSV exported.');
  };

  return <>
    <header className="topbar"><div className="topbar-title"><span className="eyebrow">{routes.overview}</span><h1>Workforce overview</h1></div><form className="overview-search" onSubmit={(e)=>{e.preventDefault();if(search.trim())go('directory',{search:search.trim()});}}><input className="inp" aria-label="Search staff" placeholder="Search name or staff ID..." value={search} onChange={(e)=>setSearch(e.target.value)}/><div className="topbar-actions"><Button onClick={()=>go('requisitions')}>Requisitions</Button><Button variant="primary" onClick={()=>go('onboard')}>Onboard employee</Button></div></form></header>
    <div className="content">
      <ApiNotice error={headcount.error} retry={headcount.reload}/>
      <div className="grid stats">
        <Stat label="Active staff" value={headcount.data ? formatCount(sum('active')) : '…'} note={`Across ${departments.length} departments`}/>
        <Stat label="On duty now" value={headcount.data ? formatCount(sum('onDuty')) : '…'} note={`From the duty roster · ${onDutyDepartments} department${onDutyDepartments === 1 ? '' : 's'}`}/>
        <Stat label="Open requisitions" value={requisitions.data ? open.length : '…'} note={<Pill kind={awaitingBudget ? 'warn' : 'ok'}>{awaitingBudget} awaiting Budget</Pill>}/>
        <Stat label="Credentials expiring ≤ 30d" value={expiring.data ? expiringRows.length : '…'} note={<Pill kind={blocking ? 'bad' : 'ok'}>{blocking} block duty eligibility</Pill>}/>
      </div>
      <div className="split">
        <Panel title="Headcount by department" action={<Button small disabled={!departments.length} onClick={exportCsv}>Export CSV</Button>} footer={<><span className="tiny">{departments.length} departments · {formatCount(sum('active'))} active · {sum('vacancies')} vacancies</span><span className="tiny mono">{payroll.data ? 'Total ' + formatMoney(payroll.data.totalAnnualSalary) + ' / year' : ''}</span></>}>
          <div className="card-body" style={{padding:'7px 14px'}}><span className="small muted">Live counts served to peer services on <span className="mono">GET /departments/headcount</span> · payroll totals on <span className="mono">GET /headcounts/payroll</span></span></div>
          <div className="table-wrap"><table className="tbl"><thead><tr><th>Department</th><th>Code</th><th>Active</th><th>On duty</th><th>Vacancies</th><th className="right">Annual payroll → Budget</th></tr></thead><tbody>{departments.map(d=><tr key={d.code}><td><button className="link" onClick={()=>go('directory',{department:d.code})}>{d.name}</button></td><td className="mono">{d.code}</td><td><div className="row nowrap"><span className="mono" style={{minWidth:38}}>{d.active}</span><div className="bar" style={{width:138}}><span style={{width:Math.max(3,d.active/maxActive*100)+'%'}}/></div></div></td><td className="mono">{d.onDuty}</td><td className="mono">{d.vacancies}</td><td className="right mono">{payrollCell(d.code)}</td></tr>)}</tbody></table>{!departments.length&&<Empty loading={headcount.loading}>No departments yet. Seed the database (see README).</Empty>}</div>
        </Panel>
        <aside className="rail">
          <Panel title="Requires attention" action={<Pill kind={attention.length ? 'bad' : 'ok'}>{attention.length}</Pill>}>{attention.map((item,i)=><div className="list-row" key={i}><div className="row"><Pill kind={item.kind}>{item.tag}</Pill><span className="eyebrow">{item.code}</span></div><p>{item.copy}</p><button className="link" onClick={()=>go(...item.to)}>{item.link}</button></div>)}{!attention.length&&<div className="list-row tiny">{expired.loading||flags.loading?'Loading…':'Nothing needs attention right now.'}</div>}</Panel>
          <Panel title="Recent changes"><div className="list-row tiny">Audit log · <span className="mono">GET /audit</span></div>{(audit.data?.items??[]).map(a=><div className="list-row" key={a.auditId}><div className="row space"><span className="mono teal small">{a.entityType} {a.action.toLowerCase()}</span><span className="tiny mono">{isoDate(a.timestampUtc)} {timeOfDay(a.timestampUtc)}</span></div><span className="tiny">{a.entityId} · by {a.actorId}</span></div>)}{audit.error&&<div className="list-row tiny">{audit.error.status===403?'Needs the Audit.Read role.':audit.error.message}</div>}<div className="notice-end">Staff.Onboarded and Staff.Terminated events leave through the transactional outbox.</div></Panel>
        </aside>
      </div>
    </div>
  </>;
}
