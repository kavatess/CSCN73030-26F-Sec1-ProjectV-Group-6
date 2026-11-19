import { useState } from 'react';
import { api, employmentTypes, employmentTypeLabel, localDate } from '../api';
import { useApi } from '../useApi';
import { Pill, Button, Panel, Select, Field, Header, ApiNotice } from '../ui';

const DRAFT_KEY = 'cmes-onboard-draft';
const today = localDate();
const blankForm = { first:'', last:'', known:'', dob:'', email:'', phone:'', citizen:'', department:'', employment:'FullTime', status:'Probation', hire:today, service:'', role:'', assignment:today, payband:'', salary:'', requisition:'' };
const blankCredential = () => ({ type:'', number:'', issued:'', expires:'' });

function loadDraft() {
  try { return JSON.parse(sessionStorage.getItem(DRAFT_KEY)) || null; } catch { return null; }
}

/** Maps API field names (incl. "initialAssignment.serviceId") to the form's keys. */
const fieldFor = { firstName:'first', lastName:'last', preferredName:'known', dateOfBirth:'dob', email:'email', phone:'phone', citizenId:'citizen', homeDepartmentCode:'department', employmentType:'employment', employmentStatus:'status', hireDate:'hire', 'initialAssignment.serviceId':'service', 'initialAssignment.roleTitle':'role', 'initialAssignment.startDate':'assignment' };

export default function Onboard({ go, notify }) {
  const draft = loadDraft();
  const [form, setForm] = useState(draft?.form ?? blankForm);
  const [credentials, setCredentials] = useState(draft?.credentials ?? [blankCredential()]);
  const [errors, setErrors] = useState({}), [problem, setProblem] = useState(null), [busy, setBusy] = useState(false), [issued, setIssued] = useState(null), [followUps, setFollowUps] = useState([]);

  const departments = useApi(() => api.departments(), []);
  const services = useApi(() => (form.department ? api.services(form.department) : Promise.resolve([])), [form.department]);
  const types = useApi(() => api.credentialTypes(), []);
  const requisitions = useApi(() => api.requisitions({ type:'Hire' }), []);
  const fillable = (requisitions.data ?? []).filter(r => ['Approved','Filling'].includes(r.status) && r.positionsFilled < r.positions);
  const requisition = fillable.find(r => r.requisitionNumber === form.requisition);

  const change = (key, value) => {
    setForm(prev => {
      const next = { ...prev, [key]:value };
      if (key === 'department') next.service = '';
      if (key === 'requisition') { const r = fillable.find(x => x.requisitionNumber === value); if (r) { next.department = r.department.code; next.service = ''; next.role = r.roleTitle; } }
      return next;
    });
    setErrors(e => ({ ...e, [key]:undefined }));
  };
  const input = (key, label, type='text', hint, required=false) => <Field label={label} hint={hint} error={errors[key]}><input className={'inp '+(errors[key]?'invalid':'')} aria-label={label} type={type} required={required} value={form[key]} onChange={e=>change(key,e.target.value)}/></Field>;
  const dropdown = (key, label, options, hint) => <Field label={label} hint={hint} error={errors[key]}><Select label={label} value={form[key]} onChange={v=>change(key,v)} options={options}/></Field>;
  const updateCredential = (i, key, value) => setCredentials(prev => prev.map((c, j) => (i === j ? { ...c, [key]:value } : c)));
  const saveDraft = () => { try { sessionStorage.setItem(DRAFT_KEY, JSON.stringify({ form, credentials })); notify('Draft kept for this browser session.'); } catch { notify('This browser does not allow saving a draft.'); } };

  const create = async () => {
    const missing = { first:'Legal first name', last:'Legal last name', email:'Work email', dob:'Date of birth', hire:'Hire date', department:'Home department' };
    const empty = Object.fromEntries(Object.entries(missing).filter(([k]) => !String(form[k]).trim()).map(([k, label]) => [k, label + ' is required.']));
    if ((form.service || form.role.trim()) && !(form.service && form.role.trim())) empty[form.service ? 'role' : 'service'] = 'Choose both a service and a role, or neither.';
    if (Object.keys(empty).length) { setErrors(empty); notify('Complete the required fields.'); return; }
    setBusy(true); setErrors({}); setProblem(null);
    let staff;
    try {
      staff = await api.createStaff({
        firstName:form.first, lastName:form.last, preferredName:form.known || null, email:form.email, dateOfBirth:form.dob, phone:form.phone || null,
        citizenId:form.citizen || null, hireDate:form.hire, employmentType:form.employment, employmentStatus:form.status, homeDepartmentCode:form.department,
        initialAssignment: form.service ? { serviceId:Number(form.service), roleTitle:form.role, startDate:form.assignment || form.hire } : null,
      });
    } catch (error) {
      const fields = Object.fromEntries(Object.entries(error.errors).map(([k, v]) => [fieldFor[k] ?? k, v.join(' ')]));
      if (error.status === 409) fields.email = error.message;
      setErrors(fields);
      if (!Object.keys(fields).length || Object.keys(fields).some(k => !Object.values(fieldFor).includes(k))) setProblem(error);
      setBusy(false);
      return;
    }
    // The employee exists now. Record the rest; report anything that fails instead of undoing the hire.
    const results = [];
    for (const c of credentials.filter(c => c.type && c.number && c.issued && c.expires)) {
      try { await api.addCredential(staff.staffNumber, { credentialTypeId:Number(c.type), number:c.number, issueDate:c.issued, expiryDate:c.expires }); results.push(['ok', `Credential ${c.number} recorded`]); }
      catch (error) { results.push(['bad', `Credential ${c.number}: ${error.message}`]); }
    }
    if (form.payband && form.salary) {
      try { await api.setCompensation(staff.staffNumber, { payBand:form.payband, annualSalary:Number(form.salary), effectiveFrom:form.hire }); results.push(['ok', `Pay band ${form.payband} recorded`]); }
      catch (error) { results.push(['bad', error.status === 403 ? 'Pay band not recorded: needs the Payroll role' : `Pay band: ${error.message}`]); }
    }
    if (requisition) {
      try {
        const filled = requisition.positionsFilled + 1;
        await api.updateRequisition(requisition.requisitionNumber, { positionsFilled:filled, ...(requisition.status === 'Approved' ? { status:'Filling' } : {}) });
        if (filled === requisition.positions) await api.updateRequisition(requisition.requisitionNumber, { status:'Completed' });
        results.push(['ok', `${requisition.requisitionNumber}: ${filled} of ${requisition.positions} positions filled`]);
      } catch (error) { results.push(['bad', `${requisition.requisitionNumber}: ${error.message}`]); }
    }
    try { sessionStorage.removeItem(DRAFT_KEY); } catch { /* storage unavailable */ }
    setIssued(staff); setFollowUps(results); setBusy(false);
    notify(`Employee created: ${staff.staffNumber}. Staff.Onboarded is queued for peer services.`);
  };

  const createButton = <Button variant="primary" disabled={busy || !!issued} onClick={create}>{busy ? 'Creating…' : 'Create employee & issue ID'}</Button>;
  return <><Header page="onboard" actions={<><Button disabled={!!issued} onClick={saveDraft}>Save draft</Button>{createButton}</>}/>
    <div className="content">
      {requisition?<div className="note info"><span><strong>Filling position {requisition.positionsFilled + 1} of {requisition.positions} on <span className="mono">{requisition.requisitionNumber}</span></strong> — {requisition.department.name} · {requisition.roleTitle}. Budget approved this requisition; no further funding check is needed. <button className="link" onClick={()=>go('requisitions',{selected:requisition.requisitionNumber})}>Open requisition</button></span></div>
        :<div className="note info"><span>Choose an approved hire requisition below to fill one of its positions, or onboard without one.</span></div>}
      <ApiNotice error={problem}/>
      <div className="split"><div className="stack">
        <Panel title="1 · Identity" action={<span className="tiny">Required before an ID can be issued</span>}><div className="form-section form-grid">{input('first','Legal first name','text',null,true)}{input('last','Legal last name','text',null,true)}{input('known','Known as','text','optional')}{input('dob','Date of birth','date',null,true)}{input('email','Work email','email','must be unique across the city',true)}{input('phone','Personal phone','tel')}{input('citizen','Citizen ID','text','matched against Residence')}{dropdown('department','Home department',[['','Choose a department'],...(departments.data??[]).map(d=>[d.code,d.name])])}</div></Panel>
        <Panel title="2 · Employment & assignment"><div className="form-section form-grid">{dropdown('requisition','Fills requisition',[['','None'],...fillable.map(r=>[r.requisitionNumber,`${r.requisitionNumber} · ${r.roleTitle} (${r.positions - r.positionsFilled} open)`])])}{dropdown('employment','Employment type',employmentTypes.map(t=>[t,employmentTypeLabel(t)]))}{dropdown('status','Employment status',['Probation','Active'])}{input('hire','Hire date','date',null,true)}{dropdown('service','Service',[['',form.department?'No assignment yet':'Choose a department first'],...(services.data??[]).map(s=>[String(s.serviceId),s.name])])}{input('role','Role title')}{input('assignment','Assignment start','date')}{input('payband','Pay band','text','HR Payroll only')}{input('salary','Annual salary','number','HR Payroll only')}</div></Panel>
        <Panel title="3 · Credentials" action={<Button small onClick={()=>setCredentials([...credentials,blankCredential()])}>Add credential</Button>}>
          <div className="card-body"><div className="note warn">Duty-blocking credentials must be valid before the employee passes the eligibility check and can be rostered. Rows left incomplete are not saved.</div></div>
          <div className="table-wrap"><table className="tbl"><thead><tr><th>Type</th><th>Number</th><th>Issued</th><th>Expires</th><th>Duty-blocking</th><th>Remove</th></tr></thead><tbody>{credentials.map((c,i)=>{const type=(types.data??[]).find(t=>String(t.credentialTypeId)===c.type);return <tr key={i}><td><Select label={'Credential type '+(i+1)} value={c.type} onChange={v=>updateCredential(i,'type',v)} options={[['','Choose a type'],...(types.data??[]).map(t=>[String(t.credentialTypeId),t.name])]}/></td><td><input className="inp" aria-label={'Credential number '+(i+1)} value={c.number} onChange={e=>updateCredential(i,'number',e.target.value)}/></td><td><input className="inp" aria-label={'Issued date '+(i+1)} type="date" value={c.issued} onChange={e=>updateCredential(i,'issued',e.target.value)}/></td><td><input className="inp" aria-label={'Expiry date '+(i+1)} type="date" value={c.expires} onChange={e=>updateCredential(i,'expires',e.target.value)}/></td><td>{type?<Pill kind={type.isDutyBlocking?'warn':'info'}>{type.isDutyBlocking?'Yes':'No'}</Pill>:'—'}</td><td><Button small onClick={()=>setCredentials(credentials.filter((_,j)=>j!==i))}>Remove</Button></td></tr>;})}</tbody></table></div>
          <div className="card-foot"><Button onClick={()=>go('directory')}>Cancel</Button><div className="row"><Button disabled={!!issued} onClick={saveDraft}>Save draft</Button>{createButton}</div></div>
        </Panel>
      </div><aside className="rail">
        <Panel title="Municipal staff ID" action={<Pill kind={issued?'ok':'clay'}>{issued?'Issued':'Not yet issued'}</Pill>}><div className="card-body"><div className="big-id"><div className="eyebrow">{issued?'ISSUED':'ALLOCATED ON SAVE'}</div>{issued?issued.staffNumber:'STF-•••••••'}</div>{issued&&<div className="row" style={{marginBottom:14}}><Button small variant="primary" onClick={()=>go('profile',{staffNumber:issued.staffNumber})}>Open profile</Button><Button small onClick={()=>{setIssued(null);setFollowUps([]);setForm(blankForm);setCredentials([blankCredential()]);}}>Onboard another</Button></div>}<dl className="kv"><dt>Format</dt><dd className="mono">STF-0000000</dd><dt>Source</dt><dd>Single municipal sequence</dd><dt>Scope</dt><dd>City-wide, all departments</dd><dt>Reuse</dt><dd>Never, even after termination</dd></dl><hr className="rule"/><p className="tiny">The ID has no department code. A transfer between departments must not change it.</p></div></Panel>
        {followUps.length>0&&<Panel title="After save">{followUps.map(([kind,text],i)=><div className="list-row" key={i}><Pill kind={kind}>{kind==='ok'?'Done':'Failed'}</Pill> <span className="small">{text}</span></div>)}</Panel>}
        <Panel title="On save"><div className="card-body">{[['1','POST /staff writes the employee and allocates the ID'],['2','The initial assignment is created in the same transaction'],['3','Staff.Onboarded is queued in the outbox'],['4','Credentials and pay band are recorded'],['5','The requisition’s filled positions go up by one']].map(([n,d])=><div className="step" key={n}><span className="step-dot">{n}</span><span>{d}</span></div>)}<p className="tiny">Every change is written to the audit log.</p></div></Panel>
      </aside></div>
    </div>
  </>;
}
