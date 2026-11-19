import { Fragment, useState } from 'react';
import { api, employmentStatuses, employmentTypes, employmentTypeLabel, formatMoney, humanize, isoDate, localDate, remainingText, sortName, timeOfDay } from '../api';
import { useApi } from '../useApi';
import { Pill, Button, Panel, Select, Field, Header, Dialog, ApiNotice, Empty } from '../ui';

const terminationReasons = [['RESIGNATION','Resignation'],['RETIREMENT','Retirement'],['END_OF_CONTRACT','End of contract'],['DISMISSAL','Dismissal']];

/** "7 yr 11 mo" between an ISO date and today. */
function serviceYears(hireDate) {
  if (!hireDate) return '—';
  const [y, m] = hireDate.split('-').map(Number);
  const now = new Date();
  const months = (now.getFullYear() - y) * 12 + (now.getMonth() + 1 - m);
  return months < 0 ? 'Starts ' + hireDate : `${Math.floor(months / 12)} yr ${months % 12} mo`;
}

function CredentialTable({ rows }) {
  return rows.length?<div className="table-wrap" style={{marginTop:14}}><table className="tbl"><thead><tr><th>Credential</th><th>Number</th><th>Expires</th><th>State</th><th>Duty impact</th></tr></thead><tbody>{rows.map(c=><tr key={c.credentialId}><td>{c.credentialType}</td><td className="mono">{c.number}</td><td className="mono">{isoDate(c.expiryDate)}</td><td><Pill>{c.status==='Expiring'?remainingText(c.daysUntilExpiry):humanize(c.status)}</Pill></td><td>{c.isDutyBlocking?'Required for duty':'Advisory'}</td></tr>)}</tbody></table></div>:<p className="muted small" style={{marginTop:14}}>No credentials on record.</p>;
}

/** Edit, assign, add-credential and termination dialogs. Field errors from a 422 appear under each field. */
function ProfileDialog({ kind, person, close, done, notify }) {
  const current = person.assignments.find(a => a.isCurrent);
  const [form, setForm] = useState({
    preferredName: person.preferredName ?? '', email: person.email, phone: person.phone ?? '',
    employmentStatus: person.employmentStatus, employmentType: person.employmentType, homeDepartmentCode: person.homeDepartment.code,
    department: current?.departmentCode ?? person.homeDepartment.code, serviceId: '', roleTitle: '', startDate: localDate(),
    credentialTypeId: '', number: '', issueDate: '', expiryDate: '',
    reasonCode: 'RESIGNATION',
  });
  const [errors, setErrors] = useState({}), [busy, setBusy] = useState(false);
  const set = (key, value) => setForm(f => ({ ...f, [key]: value, ...(key === 'department' ? { serviceId: '' } : {}) }));
  const departments = useApi(() => api.departments(), []);
  const services = useApi(() => (kind === 'assign' ? api.services(form.department) : Promise.resolve([])), [kind, form.department]);
  const types = useApi(() => (kind === 'credential' ? api.credentialTypes() : Promise.resolve([])), [kind]);
  const err = (key) => errors[key]?.join(' ');
  const input = (key, label, type = 'text', props = {}) => <Field label={label} error={err(key)}><input className={'inp '+(err(key)?'invalid':'')} type={type} value={form[key]} onChange={e=>set(key,e.target.value)} {...props}/></Field>;

  const submit = async (e) => {
    e.preventDefault(); setBusy(true); setErrors({});
    try {
      if (kind === 'edit') {
        await api.updateStaff(person.staffNumber, { preferredName: form.preferredName, email: form.email, phone: form.phone, employmentStatus: form.employmentStatus, employmentType: form.employmentType, homeDepartmentCode: form.homeDepartmentCode });
        notify('Profile saved.');
      } else if (kind === 'assign') {
        await api.addAssignment(person.staffNumber, { serviceId: Number(form.serviceId), roleTitle: form.roleTitle, startDate: form.startDate });
        notify('Assignment added.');
      } else if (kind === 'credential') {
        await api.addCredential(person.staffNumber, { credentialTypeId: Number(form.credentialTypeId), number: form.number, issueDate: form.issueDate, expiryDate: form.expiryDate });
        notify('Credential recorded.');
      } else {
        const created = await api.raiseRequisition({ type: 'Termination', staffNumber: person.staffNumber, departmentCode: person.homeDepartment.code, reasonCode: form.reasonCode });
        notify(`Termination requisition ${created.requisitionNumber} raised for HR review.`);
      }
      done();
    } catch (error) {
      setErrors(error.errors ?? {});
      if (!Object.keys(error.errors ?? {}).length) notify(error.message);
      setBusy(false);
    }
  };

  const title = { edit: 'Edit profile', assign: 'Assign to service', credential: 'Add credential', termination: 'Request termination' }[kind];
  const action = { edit: 'Save changes', assign: 'Add assignment', credential: 'Record credential', termination: 'Raise termination requisition' }[kind];
  return <Dialog title={title} action={action} busy={busy} close={close} submit={submit}>
    {kind==='edit'&&<div className="form-grid">{input('preferredName','Known as')}{input('email','Work email','email',{required:true})}{input('phone','Phone','tel')}
      <Field label="Employment status" error={err('employmentStatus')}><Select label="Employment status" value={form.employmentStatus} onChange={v=>set('employmentStatus',v)} options={employmentStatuses.filter(s=>s!=='Terminated'||person.employmentStatus==='Terminated').map(s=>[s,humanize(s)])}/></Field>
      <Field label="Employment type" error={err('employmentType')}><Select label="Employment type" value={form.employmentType} onChange={v=>set('employmentType',v)} options={employmentTypes.map(t=>[t,employmentTypeLabel(t)])}/></Field>
      <Field label="Home department" error={err('homeDepartmentCode')}><Select label="Home department" value={form.homeDepartmentCode} onChange={v=>set('homeDepartmentCode',v)} options={(departments.data??[]).map(d=>[d.code,d.name])}/></Field></div>}
    {kind==='assign'&&<div className="form-grid"><Field label="Department"><Select label="Assignment department" value={form.department} onChange={v=>set('department',v)} options={(departments.data??[]).map(d=>[d.code,d.name])}/></Field>
      <Field label="Service" error={err('serviceId')}><Select label="Assignment service" required value={form.serviceId} onChange={v=>set('serviceId',v)} options={[['','Choose a service'],...(services.data??[]).map(s=>[String(s.serviceId),s.name])]}/></Field>
      {input('roleTitle','Role title','text',{required:true})}{input('startDate','Start date','date',{required:true})}</div>}
    {kind==='credential'&&<div className="form-grid"><Field label="Credential type" error={err('credentialTypeId')}><Select label="Credential type" required value={form.credentialTypeId} onChange={v=>set('credentialTypeId',v)} options={[['','Choose a type'],...(types.data??[]).map(t=>[String(t.credentialTypeId),t.name])]}/></Field>
      {input('number','Credential number','text',{required:true})}{input('issueDate','Issued','date',{required:true})}{input('expiryDate','Expires','date',{required:true})}</div>}
    {kind==='termination'&&<><div className="note warn">This raises a Termination requisition for HR review. The employee is terminated, and Staff.Terminated is published, only when the requisition is completed.</div>
      <Field label="Reason" error={err('reasonCode')}><Select label="Termination reason" value={form.reasonCode} onChange={v=>set('reasonCode',v)} options={terminationReasons}/></Field></>}
  </Dialog>;
}

export default function Profile({ staffNumber, go, notify }) {
  const [tab, setTab] = useState('Overview'), [modal, setModal] = useState('');
  const person = useApi(() => api.staff(staffNumber), [staffNumber]);
  const eligibility = useApi(() => api.eligibility(staffNumber), [staffNumber, person.data]);
  const flags = useApi(() => api.securityFlags(staffNumber), [staffNumber]);
  const compensation = useApi(() => (tab === 'Compensation' ? api.compensation(staffNumber) : Promise.resolve(null)), [staffNumber, tab]);
  const audit = useApi(() => (tab === 'Audit trail' ? api.audit({ entityId: staffNumber, pageSize: 50 }) : Promise.resolve(null)), [staffNumber, tab, person.data]);
  const shifts = useApi(() => (tab === 'Roster' ? api.rosters({ from: localDate(), to: localDate(new Date(Date.now() + 14 * 86400000)) }) : Promise.resolve([])), [tab]);
  const [ending, setEnding] = useState(null);

  if (!person.data) return <><Header page="profile" route={'← STAFF DIRECTORY / GET /STAFF/' + staffNumber} title={staffNumber || 'Employee profile'} actions={<Button onClick={()=>go('directory')}>Back to directory</Button>}/>
    <div className="content">{person.error?<ApiNotice error={person.error} retry={person.error.status===404?null:person.reload}/>:<Empty loading>Loading…</Empty>}</div></>;

  const p = person.data;
  const current = p.assignments.filter(a => a.isCurrent);
  const activeFlags = (flags.data ?? []).filter(f => f.status === 'Active');
  const refresh = () => { setModal(''); person.reload(); flags.reload(); };
  const endAssignment = async (a) => {
    setEnding(a.assignmentId);
    try { await api.updateAssignment(a.assignmentId, { endDate: localDate() }); notify(`${a.roleTitle} assignment ended today.`); person.reload(); }
    catch (error) { notify(error.message); }
    setEnding(null);
  };
  const terminated = p.employmentStatus === 'Terminated';

  return <><Header page="profile" route={'← STAFF DIRECTORY / GET /STAFF/' + p.staffNumber} title={sortName(p)} actions={<><Button onClick={()=>setModal('edit')}>Edit profile</Button><Button disabled={terminated} onClick={()=>setModal('assign')}>Assign to service</Button><Button variant="danger" disabled={terminated} onClick={()=>setModal('termination')}>Request termination</Button></>}/>
    <div className="content"><div className="row"><span className="chip mono">{p.staffNumber}</span><Button small onClick={()=>{navigator.clipboard?.writeText(p.staffNumber);notify('Staff ID copied.');}}>Copy ID</Button><Pill>{humanize(p.employmentStatus)}</Pill>{p.credentialState&&p.credentialState!=='Valid'&&<Pill>{'Credentials '+humanize(p.credentialState).toLowerCase()}</Pill>}</div>
      <div className="split"><Panel title={<span className="eyebrow">Employee record</span>}><div className="tabs" role="tablist" aria-label="Employee record">{['Overview','Assignments','Credentials','Roster','Compensation','Audit trail'].map(t=><button key={t} role="tab" aria-selected={tab===t} onClick={()=>setTab(t)}>{t}</button>)}</div>
        {tab==='Overview'&&<div className="card-body"><div className="profile-summary">
          <div><span className="eyebrow">Identity</span><dl className="kv"><dt>Legal name</dt><dd>{p.firstName} {p.lastName}</dd><dt>Known as</dt><dd>{p.preferredName||'—'}</dd><dt>Work email</dt><dd className="mono tiny">{p.email}</dd><dt>Phone</dt><dd className="mono">{p.phone||'—'}</dd><dt>Date of birth</dt><dd className="mono">{p.dateOfBirth}</dd><dt>Citizen ID</dt><dd className="mono">{p.citizenId||'—'}</dd></dl></div>
          <div><span className="eyebrow">Employment</span><dl className="kv"><dt>Status</dt><dd><Pill>{humanize(p.employmentStatus)}</Pill></dd><dt>Type</dt><dd>{employmentTypeLabel(p.employmentType)}</dd><dt>Hire date</dt><dd className="mono">{p.hireDate}</dd><dt>Service years</dt><dd>{serviceYears(p.hireDate)}</dd><dt>Home department</dt><dd>{p.homeDepartment.name}</dd></dl></div>
          <div><span className="eyebrow">Current posting</span>{current.length?current.map(a=><dl className="kv" key={a.assignmentId}><dt>Service</dt><dd>{a.serviceName} <span className="mono tiny">{a.departmentCode}</span></dd><dt>Role</dt><dd>{a.roleTitle}</dd><dt>Since</dt><dd className="mono">{a.startDate}</dd></dl>):<p className="muted small">No current assignment.</p>}</div>
        </div><hr className="rule"/><div className="eyebrow">Credentials held</div><CredentialTable rows={p.credentials}/></div>}
        {tab==='Credentials'&&<div className="card-body"><div className="eyebrow">Credentials held</div><CredentialTable rows={p.credentials}/><div className="row" style={{marginTop:14}}><Button small variant="primary" disabled={terminated} onClick={()=>setModal('credential')}>Add credential</Button><Button small onClick={()=>go('credentials')}>Open credential tracker</Button></div></div>}
        {tab==='Assignments'&&<div className="card-body"><div className="table-wrap"><table className="tbl"><thead><tr><th>Service</th><th>Role</th><th>Start</th><th>End</th><th>State</th><th>Actions</th></tr></thead><tbody>{p.assignments.map(a=><tr key={a.assignmentId}><td>{a.serviceName} <span className="mono tiny">{a.departmentCode}</span></td><td>{a.roleTitle}</td><td className="mono">{a.startDate}</td><td className="mono">{a.endDate||'—'}</td><td><Pill kind={a.isCurrent?'ok':'info'}>{a.isCurrent?'Current':'Ended'}</Pill></td><td>{a.isCurrent&&!a.endDate&&<Button small disabled={ending===a.assignmentId} onClick={()=>endAssignment(a)}>End today</Button>}</td></tr>)}</tbody></table>{!p.assignments.length&&<Empty>No assignments yet.</Empty>}</div><div style={{marginTop:14}}><Button small variant="primary" disabled={terminated} onClick={()=>setModal('assign')}>Assign to service</Button></div></div>}
        {tab==='Roster'&&<div className="card-body"><h3>Shifts in the next 14 days</h3>{(()=>{const mine=(shifts.data??[]).filter(s=>s.staffNumber===p.staffNumber);return mine.length?<div className="table-wrap" style={{marginTop:14}}><table className="tbl"><thead><tr><th>Date</th><th>Shift</th><th>Time</th><th>Service</th><th>Status</th></tr></thead><tbody>{mine.map(s=><tr key={s.rosterEntryId}><td className="mono">{localDate(new Date(s.shiftStart))}</td><td>{s.shiftType}</td><td className="mono">{timeOfDay(s.shiftStart)}–{timeOfDay(s.shiftEnd)}</td><td>{s.serviceName}</td><td><Pill>{s.status}</Pill></td></tr>)}</tbody></table></div>:<p style={{margin:'10px 0 14px'}} className="muted">{shifts.loading?'Loading…':'No shifts scheduled.'}</p>;})()}<Button small onClick={()=>go('roster')}>Open duty roster</Button></div>}
        {tab==='Compensation'&&<div className="card-body">{compensation.error?.status===403?<div className="note info">Individual salary belongs to HR Payroll (StaffHr.Payroll role). The Staff directory and peer services receive department totals only.</div>:compensation.error?<ApiNotice error={compensation.error}/>:<><div className="table-wrap"><table className="tbl"><thead><tr><th>Pay band</th><th className="right">Annual salary</th><th>From</th><th>To</th></tr></thead><tbody>{(compensation.data??[]).map(c=><tr key={c.compensationId}><td className="mono">{c.payBand}</td><td className="right mono">{formatMoney(c.annualSalary)}</td><td className="mono">{c.effectiveFrom}</td><td className="mono">{c.effectiveTo||'Current'}</td></tr>)}</tbody></table>{!compensation.data?.length&&<Empty loading={compensation.loading}>No compensation recorded.</Empty>}</div><p className="tiny" style={{marginTop:14}}>Visible to the Payroll role only. Never included in staff or search responses.</p></>}</div>}
        {tab==='Audit trail'&&<div className="card-body">{audit.error?<div className="note">{audit.error.status===403?'The audit trail needs the Audit.Read role.':audit.error.message}</div>:<div className="table-wrap"><table className="tbl"><thead><tr><th>When (UTC)</th><th>Record</th><th>Action</th><th>By</th></tr></thead><tbody>{(audit.data?.items??[]).map(a=><tr key={a.auditId}><td className="mono nowrap">{a.timestampUtc.slice(0,16).replace('T',' ')}</td><td>{a.entityType} <span className="mono tiny">{a.entityId}</span></td><td><Pill kind="info">{a.action}</Pill></td><td className="mono">{a.actorId}</td></tr>)}</tbody></table>{!audit.data?.items?.length&&<Empty loading={audit.loading}>No audit entries.</Empty>}</div>}</div>}
      </Panel><aside className="rail">
        <Panel title="Duty eligibility">{eligibility.data?<div className="card-body stack"><div><Pill kind={eligibility.data.eligible?'ok':'bad'}>{eligibility.data.eligible?'Eligible for duty':'Not eligible for duty'}</Pill></div>{eligibility.data.reasons.map(r=><div key={r.code+r.message}>✗ {r.message}</div>)}{eligibility.data.warnings.map(w=><div key={w.code+w.message}>! {w.message}</div>)}{!eligibility.data.reasons.length&&!eligibility.data.warnings.length&&<div>✓ Status, credentials and security flags all pass</div>}<hr className="rule"/><span className="tiny">Answered on <span className="mono">GET /staff/{p.staffNumber}/eligibility</span>. Peer services call it before rostering or dispatch.</span></div>:<div className="card-body tiny">{eligibility.error?eligibility.error.message:'Checking…'}</div>}</Panel>
        <Panel title="Security flags" action={<Pill kind={activeFlags.length?'bad':'ok'}>{activeFlags.length?activeFlags.length+' active':'Clear'}</Pill>}><div className="card-body">{(flags.data??[]).length?<dl className="kv">{flags.data.map(f=><Fragment key={f.flagId}><dt>{humanize(f.flagType)}</dt><dd><Pill kind={f.status==='Active'?'bad':'ok'}>{f.status}</Pill> <span className="tiny mono">{isoDate(f.updatedAt)}</span></dd></Fragment>)}</dl>:<p className="small muted">{flags.loading?'Loading…':'No flags on record.'}</p>}<p className="tiny" style={{marginTop:18}}>Security is the source of record and syncs flags through <span className="mono">PUT /staff/…/security-flags</span>. HR cannot edit them here.</p></div></Panel>
        <Panel title="Record shared with">{[['Public Transport','roster, certification'],['Security','flags, eligibility'],['Budget','headcount only'],['Residence','citizen ID match']].map(([n,d])=><div className="side-panel-row" key={n}><span>{n}</span><span className="tiny mono">{d}</span></div>)}</Panel>
      </aside></div>
    </div>
    {modal&&<ProfileDialog kind={modal} person={p} close={()=>setModal('')} done={refresh} notify={notify}/>}
  </>;
}
