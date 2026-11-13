import { useState } from 'react';
import { api, humanize, localDate, sortName, timeOfDay } from '../api';
import { useApi } from '../useApi';
import { Pill, Button, Panel, Select, Field, Header, Dialog, ApiNotice, Empty } from '../ui';

/** Shift presets: start hour and length in hours, in local time. */
const presets = { Day: [7, 12], Night: [19, 12], Evening: [15, 8], OnCall: [7, 12] };
const letter = { Day: 'D', Evening: 'E', Night: 'N', OnCall: 'O' };
/** Planning target per day used by the coverage strip. */
const target = { Day: 2, Night: 2 };
const DAY = 86400000;

/** Monday 00:00 (local) of the week `offset` weeks from this one. */
function weekStart(offset) {
  const now = new Date();
  const monday = new Date(now.getFullYear(), now.getMonth(), now.getDate() - ((now.getDay() + 6) % 7));
  monday.setDate(monday.getDate() + offset * 7);
  return monday;
}
const addDays = (date, n) => new Date(date.getFullYear(), date.getMonth(), date.getDate() + n);
const dayLabel = (d, opts) => d.toLocaleDateString('en-CA', opts);
const hours = (e) => (new Date(e.shiftEnd) - new Date(e.shiftStart)) / 3600000;

function ShiftDialog({ service, staff, initial, close, done, notify }) {
  const [form, setForm] = useState(initial);
  const [problem, setProblem] = useState(null), [busy, setBusy] = useState(false);
  const set = (key, value) => setForm(f => ({ ...f, [key]: value }));
  const submit = async (e) => {
    e.preventDefault(); setBusy(true); setProblem(null);
    const [startHour, length] = presets[form.shiftType];
    const [y, m, d] = form.date.split('-').map(Number);
    const start = new Date(y, m - 1, d, startHour), end = new Date(start.getTime() + length * 3600000);
    try {
      // Same check peer services make: an ineligible person can't be rostered from this screen.
      const check = await api.eligibility(form.staffNumber, { service: service.serviceId, at: start.toISOString() });
      if (!check.eligible) { setProblem({ message: 'Not eligible for duty: ' + check.reasons.map(r => r.message).join(' ') }); setBusy(false); return; }
      await api.addRosterEntry({ staffNumber: form.staffNumber, serviceId: service.serviceId, shiftStart: start.toISOString(), shiftEnd: end.toISOString(), shiftType: form.shiftType });
      notify(`${form.shiftType} shift added for ${form.staffNumber} on ${form.date}.`);
      done();
    } catch (error) { setProblem(error); setBusy(false); }
  };
  return <Dialog title={'Add shift · ' + service.name} action="Add shift" busy={busy} close={close} submit={submit}>
    <ApiNotice error={problem}/>
    <div className="form-grid">
      <Field label="Employee"><Select label="Employee" required value={form.staffNumber} onChange={v=>set('staffNumber',v)} options={[['','Choose an employee'],...staff.map(p=>[p.staffNumber,`${sortName(p)} · ${p.staffNumber}`])]}/></Field>
      <Field label="Date"><input className="inp" aria-label="Shift date" type="date" required value={form.date} onChange={e=>set('date',e.target.value)}/></Field>
      <Field label="Shift" hint={form.shiftType?`${String(presets[form.shiftType][0]).padStart(2,'0')}:00 for ${presets[form.shiftType][1]} h`:''}><Select label="Shift type" value={form.shiftType} onChange={v=>set('shiftType',v)} options={Object.keys(presets).map(t=>[t,humanize(t)])}/></Field>
    </div>
    <div className="note info">The employee's duty eligibility for this service is checked before the shift is saved. Overlapping shifts are refused (409).</div>
  </Dialog>;
}

export default function Roster({ go, notify }) {
  const [week, setWeek] = useState(0), [department, setDepartment] = useState('EMS'), [serviceId, setServiceId] = useState('1'), [adding, setAdding] = useState(null), [busy, setBusy] = useState(false);
  const from = weekStart(week), to = addDays(from, 7);
  const days = [...Array(7)].map((_, i) => addDays(from, i));
  const departments = useApi(() => api.departments(), []);
  const services = useApi(() => api.services(department), [department]);
  const service = (services.data ?? []).find(s => String(s.serviceId) === serviceId);
  const entries = useApi(() => (serviceId ? api.rosters({ service: serviceId, from: from.toISOString(), to: to.toISOString() }) : Promise.resolve([])), [serviceId, week]);
  const people = useApi(() => (serviceId ? api.searchStaff({ service: serviceId, pageSize: 100 }) : Promise.resolve({ items: [] })), [serviceId]);
  const staff = (people.data?.items ?? []).filter(p => p.employmentStatus !== 'Terminated');
  const eligibility = useApi(() => Promise.all(staff.map(p => api.eligibility(p.staffNumber, { service: serviceId }))), [serviceId, staff.map(p => p.staffNumber)]);
  const checks = Object.fromEntries((eligibility.data ?? []).map(e => [e.staffNumber, e]));

  const shifts = (entries.data ?? []).filter(e => e.status !== 'Cancelled');
  // Rows: everyone assigned to the service, plus anyone rostered on it this week.
  const rows = [...staff.map(p => ({ staffNumber: p.staffNumber, name: sortName(p), detail: p.currentRole })),
    ...[...new Map((entries.data ?? []).filter(e => !staff.some(p => p.staffNumber === e.staffNumber)).map(e => [e.staffNumber, { staffNumber: e.staffNumber, name: e.staffName, detail: 'Not assigned to this service' }])).values()]];
  const startsOn = (e, day) => localDate(new Date(e.shiftStart)) === localDate(day);
  const coverage = days.map(day => Object.fromEntries(Object.keys(target).map(type => [type, shifts.filter(e => e.shiftType === type && startsOn(e, day)).length])));
  // Days already over are history, not gaps.
  const past = days.map(d => localDate(d) < localDate());
  const short = coverage.map((c, i) => (past[i] ? 0 : Object.entries(target).reduce((n, [type, want]) => n + Math.max(0, want - c[type]), 0)));
  const eligible = staff.filter(p => checks[p.staffNumber]?.eligible), blocked = staff.filter(p => checks[p.staffNumber] && !checks[p.staffNumber].eligible);
  const firstGap = short.findIndex(n => n > 0);
  const totalHours = shifts.reduce((n, e) => n + hours(e), 0);
  const refresh = () => { setAdding(null); entries.reload(); };
  const openAdd = (staffNumber = '', day = days[Math.max(0, firstGap)], shiftType = 'Day') => setAdding({ staffNumber, date: localDate(day), shiftType });

  const remove = async (e) => {
    if (!window.confirm(`Remove ${e.staffName}'s ${e.shiftType.toLowerCase()} shift on ${localDate(new Date(e.shiftStart))}?`)) return;
    try { await api.deleteRosterEntry(e.rosterEntryId); notify('Shift removed.'); entries.reload(); } catch (error) { notify(error.message); }
  };
  const copyLastWeek = async () => {
    setBusy(true);
    try {
      const previous = await api.rosters({ service: serviceId, from: addDays(from, -7).toISOString(), to: from.toISOString() });
      let copied = 0, skipped = 0;
      for (const e of previous.filter(x => x.status !== 'Cancelled')) {
        try {
          await api.addRosterEntry({ staffNumber: e.staffNumber, serviceId: e.serviceId, shiftType: e.shiftType,
            shiftStart: new Date(new Date(e.shiftStart).getTime() + 7 * DAY).toISOString(), shiftEnd: new Date(new Date(e.shiftEnd).getTime() + 7 * DAY).toISOString() });
          copied++;
        } catch { skipped++; }
      }
      notify(previous.length ? `Copied ${copied} shift${copied === 1 ? '' : 's'} from last week${skipped ? `; ${skipped} skipped (overlap or no longer valid)` : ''}.` : 'Last week has no shifts to copy.');
      entries.reload();
    } catch (error) { notify(error.message); }
    setBusy(false);
  };

  return <><Header page="roster" route={`GET /ROSTERS?SERVICE=${serviceId}&FROM=${localDate(from)}&TO=${localDate(to)}`} actions={<><Button disabled={busy||!serviceId} onClick={copyLastWeek}>{busy?'Copying…':'Copy last week'}</Button><Button variant="primary" disabled={!service} onClick={()=>openAdd()}>Add shift</Button></>}/>
    <div className="content">
      <div className="card card-body row space"><div className="row"><Field label="Department"><Select label="Roster department" value={department} onChange={v=>{setDepartment(v);setServiceId('');}} options={(departments.data??[]).map(d=>[d.code,d.name])}/></Field><Field label="Service"><Select label="Roster service" value={serviceId} onChange={setServiceId} options={[['','Choose a service'],...(services.data??[]).map(s=>[String(s.serviceId),s.name])]}/></Field></div><div className="row"><Button small aria-label="Previous week" onClick={()=>setWeek(week-1)}>‹</Button><span className="mono small">{dayLabel(days[0],{weekday:'short',day:'numeric'})} – {dayLabel(days[6],{weekday:'short',day:'numeric',month:'short',year:'numeric'})}</span><Button small aria-label="Next week" onClick={()=>setWeek(week+1)}>›</Button>{week!==0&&<Button small onClick={()=>setWeek(0)}>This week</Button>}</div></div>
      <ApiNotice error={entries.error} retry={entries.reload}/>
      {serviceId&&firstGap>=0&&!entries.loading&&<div className="note bad">■ <span><strong>{dayLabel(days[firstGap],{weekday:'long',day:'numeric',month:'long'})} is {short[firstGap]} short</strong> of the planning target ({target.Day} day · {target.Night} night). Peer services read the reduced crew count from <span className="mono">GET /rosters/headcount</span>.</span></div>}
      <div className="split"><div className="stack">
        <Panel title="Coverage · scheduled vs target" action={<span className="tiny">Target per day: Day {target.Day} · Night {target.Night}</span>}><div className="coverage">{days.map((d,i)=><div key={i} className={'coverage-day '+(short[i]?'alert':'')}><div className="eyebrow">{dayLabel(d,{weekday:'short',day:'numeric'}).toUpperCase()}</div><div className="number">{coverage[i].Day+coverage[i].Night} / {target.Day+target.Night}</div>{past[i]?<Pill kind="info">Past</Pill>:<Pill kind={short[i]?'bad':'ok'}>{short[i]?short[i]+' short':'Met'}</Pill>}</div>)}</div></Panel>
        <Panel title={(service?.name??'Roster')+' · '+rows.length+' staff'} action={<span className="tiny mono">D 07–19 &nbsp; N 19–07 &nbsp; E 15–23 &nbsp; O on call</span>} footer={<><span className="tiny">{shifts.length} shifts · {totalHours} hours scheduled this week</span><span className="tiny">Click a shift to remove it</span></>}><div className="table-wrap"><table className="tbl calendar-table"><thead><tr><th>Staff</th>{days.map((d,i)=><th key={i}>{dayLabel(d,{weekday:'short',day:'numeric'})}</th>)}<th>Hrs</th></tr></thead><tbody>{rows.map(p=>{const mine=shifts.filter(e=>e.staffNumber===p.staffNumber);const check=checks[p.staffNumber];return <tr key={p.staffNumber}><td><div className="person"><span className="person-avatar">{p.name.split(/,? /).slice(0,2).map(x=>x[0]).join('')}</span><div><button className="link mono small" onClick={()=>go('profile',{staffNumber:p.staffNumber})}>{p.name.toUpperCase()}</button><div className="tiny mono">{p.detail}{check&&!check.eligible?' · not eligible':''}</div></div></div></td>{days.map((d,i)=>{const today=mine.filter(e=>startsOn(e,d));return <td key={i}>{today.length?today.map(e=><button type="button" key={e.rosterEntryId} title={`${e.shiftType} ${timeOfDay(e.shiftStart)}–${timeOfDay(e.shiftEnd)} · ${e.status} · click to remove`} style={{cursor:'pointer',margin:1}} className={'shift '+(e.shiftType==='Evening'?'evening':e.shiftType==='Night'?'night':'')} onClick={()=>remove(e)}>{letter[e.shiftType]}<span>{timeOfDay(e.shiftStart).slice(0,2)}–{timeOfDay(e.shiftEnd).slice(0,2)}</span></button>):<span className="shift empty">—</span>}</td>;})}<td className="mono">{mine.reduce((n,e)=>n+hours(e),0)}</td></tr>;})}</tbody></table>{!rows.length&&<Empty loading={entries.loading||people.loading}>{serviceId?'Nobody is assigned to this service.':'Choose a service to see its roster.'}</Empty>}</div></Panel>
      </div><aside className="rail">
        <Panel title={firstGap>=0?'Fill '+dayLabel(days[firstGap],{weekday:'short',day:'numeric'}):'Eligible to roster'} action={<Pill kind={firstGap>=0?'bad':'ok'}>{firstGap>=0?short[firstGap]+' needed':'Covered'}</Pill>}>{eligible.map((p,i)=><div className="side-panel-row" key={p.staffNumber}><div><strong>{sortName(p)}</strong><div className="tiny">{p.currentRole} · {shifts.filter(e=>e.staffNumber===p.staffNumber).reduce((n,e)=>n+hours(e),0)} h this week{checks[p.staffNumber].warnings.length?' · '+checks[p.staffNumber].warnings.length+' warning':''}</div></div><Button small variant={i===0?'primary':''} onClick={()=>openAdd(p.staffNumber)}>Assign</Button></div>)}{!eligible.length&&<div className="list-row tiny">{eligibility.loading?'Checking eligibility…':'Nobody on this service is eligible right now.'}</div>}<div className="notice-end">Candidates pass <span className="mono">GET /staff/…/eligibility</span> for this service.</div></Panel>
        <Panel title="Blocked from this roster">{blocked.map(p=><div className="list-row" key={p.staffNumber}><strong>{sortName(p)}</strong><div className="tiny">{checks[p.staffNumber].reasons.map(r=>r.message).join(' ')}</div></div>)}{!blocked.length&&<div className="list-row tiny">{eligibility.loading?'Checking eligibility…':'No one is blocked.'}</div>}<div className="notice-end">HR cannot override eligibility failures here.</div></Panel>
        <Panel title="Consumed by">{[['EMS','GET /rosters/headcount'],['SEC','GET /rosters/on-duty'],['PTR','GET /staff/{id}/eligibility']].map(([n,d])=><div className="side-panel-row" key={n}><strong>{n}</strong><span className="mono tiny">{d}</span></div>)}</Panel>
      </aside></div>
    </div>
    {adding&&service&&<ShiftDialog service={service} staff={eligible.length?eligible:staff} initial={adding} close={()=>setAdding(null)} done={refresh} notify={notify}/>}
  </>;
}
