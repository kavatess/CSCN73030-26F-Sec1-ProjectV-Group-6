import { useState } from 'react';
import { api, daysFromToday, humanize, isoDate, newIdempotencyKey, openRequisitionStatuses, requisitionStatuses, requisitionStatusLabel } from '../api';
import { useApi } from '../useApi';
import { Pill, Button, Stat, Panel, Select, Field, Header, Dialog, ApiNotice, Empty } from '../ui';
import { csvDownload } from '../screens';

/** The stages a requisition passes through, per type (mirrors Domain/RequisitionWorkflow.cs). */
const stages = {
  Hire: ['Received', 'HRReview', 'AwaitingBudget', 'Approved', 'Filling', 'Completed'],
  Termination: ['Received', 'HRReview', 'Approved', 'Completed'],
};
const hireReasons = [['VACANCY','Vacancy'],['REPLACEMENT','Replacement'],['GROWTH','Growth'],['SERVICE_EXPANSION','Service expansion'],['NEW_PROGRAM','New program'],['SEASONAL','Seasonal']];
const terminationReasons = [['RESIGNATION','Resignation'],['RETIREMENT','Retirement'],['END_OF_CONTRACT','End of contract'],['DISMISSAL','Dismissal']];
const BUDGET_SLA_DAYS = 5;

/** The buttons offered for a requisition in its current state, as [label, PATCH body, variant]. */
function actions(r) {
  const hire = r.type === 'Hire';
  const close = ['Close', { status: 'Closed' }, ''];
  switch (r.status) {
    case 'Received': return [['Start HR review', { status: 'HRReview' }, 'primary'], ['Reject', { status: 'Rejected' }, 'danger'], close];
    case 'HRReview': return hire
      ? [['Send to Budget', { status: 'AwaitingBudget', budgetDecision: 'Pending' }, 'primary'], ['Reject', { status: 'Rejected' }, 'danger'], close]
      : [['Approve termination', { status: 'Approved' }, 'primary'], ['Reject', { status: 'Rejected' }, 'danger'], close];
    case 'AwaitingBudget': return [['Budget approved', { budgetDecision: 'Approved', status: 'Approved' }, 'primary'], ['Budget rejected', { budgetDecision: 'Rejected', status: 'Rejected' }, 'danger'], close];
    case 'Approved': return hire ? [['Start filling', { status: 'Filling' }, 'primary'], close] : [['Complete termination', { status: 'Completed' }, 'danger'], close];
    case 'Filling': return r.positionsFilled >= r.positions ? [['Mark completed', { status: 'Completed' }, 'primary'], close] : [close];
    case 'Completed': return [close];
    default: return [];
  }
}

function RaiseDialog({ departments, close, done, notify }) {
  const [form, setForm] = useState({ type: 'Hire', departmentCode: departments[0]?.code ?? '', roleTitle: '', positions: 1, reasonCode: 'VACANCY', staffNumber: '' });
  // One key per dialog: retrying after a network error can't create a duplicate requisition.
  const [key] = useState(newIdempotencyKey);
  const [errors, setErrors] = useState({}), [problem, setProblem] = useState(null), [busy, setBusy] = useState(false);
  const set = (k, v) => setForm(f => ({ ...f, [k]: v, ...(k === 'type' ? { reasonCode: v === 'Hire' ? 'VACANCY' : 'RESIGNATION' } : {}) }));
  const hire = form.type === 'Hire';
  const submit = async (e) => {
    e.preventDefault(); setBusy(true); setErrors({}); setProblem(null);
    const body = hire
      ? { type: 'Hire', departmentCode: form.departmentCode, roleTitle: form.roleTitle, positions: Number(form.positions), reasonCode: form.reasonCode }
      : { type: 'Termination', departmentCode: form.departmentCode, staffNumber: form.staffNumber.trim().toUpperCase(), reasonCode: form.reasonCode };
    try {
      const created = await api.raiseRequisition(body, key);
      notify(`Requisition ${created.requisitionNumber} raised.`);
      done(created.requisitionNumber);
    } catch (error) {
      setErrors(Object.fromEntries(Object.entries(error.errors).map(([k, v]) => [k, v.join(' ')])));
      if (!Object.keys(error.errors).length) setProblem(error);
      setBusy(false);
    }
  };
  return <Dialog title="Raise requisition" action="Raise requisition" busy={busy} close={close} submit={submit}>
    <ApiNotice error={problem}/>
    <div className="form-grid">
      <Field label="Type"><Select label="Requisition type" value={form.type} onChange={v=>set('type',v)} options={['Hire','Termination']}/></Field>
      <Field label="Department" error={errors.departmentCode}><Select label="Requesting department" value={form.departmentCode} onChange={v=>set('departmentCode',v)} options={departments.map(d=>[d.code,d.name])}/></Field>
      {hire?<><Field label="Role" error={errors.roleTitle}><input required className="inp" aria-label="Role" value={form.roleTitle} onChange={e=>set('roleTitle',e.target.value)}/></Field>
        <Field label="Positions" error={errors.positions}><input required className="inp" aria-label="Positions" type="number" min="1" max="100" value={form.positions} onChange={e=>set('positions',e.target.value)}/></Field></>
        :<Field label="Staff number" error={errors.staffNumber}><input required className="inp mono" aria-label="Staff number" placeholder="STF-0000000" value={form.staffNumber} onChange={e=>set('staffNumber',e.target.value)}/></Field>}
      <Field label="Reason" error={errors.reasonCode}><Select label="Reason" value={form.reasonCode} onChange={v=>set('reasonCode',v)} options={hire?hireReasons:terminationReasons}/></Field>
    </div>
    <div className="note info">Sent with an Idempotency-Key, so submitting twice never creates two requisitions.</div>
  </Dialog>;
}

export default function Requisitions({ go, notify, filter = {} }) {
  const [type, setType] = useState(''), [status, setStatus] = useState('open'), [dept, setDept] = useState(''), [selected, setSelected] = useState(filter.selected || ''), [raising, setRaising] = useState(false), [busy, setBusy] = useState(false);
  const departments = useApi(() => api.departments(), []);
  const all = useApi(() => api.requisitions(), []);
  const list = useApi(() => api.requisitions({ type, department: dept, status: status === 'open' ? '' : status }), [type, dept, status]);
  const rows = (list.data ?? []).filter(r => status !== 'open' || openRequisitionStatuses.includes(r.status));
  const chosen = (list.data ?? []).concat(all.data ?? []).find(r => r.requisitionNumber === selected) || rows[0];
  const everything = all.data ?? [];
  const open = everything.filter(r => openRequisitionStatuses.includes(r.status));
  const awaiting = open.filter(r => r.status === 'AwaitingBudget');
  const oldest = Math.max(0, ...awaiting.map(r => -daysFromToday(r.createdAtUtc)));
  const unfilled = open.filter(r => r.type === 'Hire' && ['Approved', 'Filling'].includes(r.status));
  const reload = () => { list.reload(); all.reload(); };

  const move = async (r, body) => {
    if (body.status === 'Completed' && r.type === 'Termination' && !window.confirm(`Complete the termination of ${r.staffNumber}? This ends their assignments and future shifts and publishes Staff.Terminated.`)) return;
    setBusy(true);
    try { await api.updateRequisition(r.requisitionNumber, body); notify(`${r.requisitionNumber} is now ${requisitionStatusLabel(body.status)}.`); reload(); }
    catch (error) { notify(error.message); }
    setBusy(false);
  };

  const exportCsv = () => {
    csvDownload('cmes-requisitions.csv', ['Requisition','Type','Department','Role','Staff','Positions','Filled','Budget','Status','Reason','Raised'],
      rows.map(r => [r.requisitionNumber, r.type, r.department.name, r.roleTitle, r.staffNumber, r.positions, r.positionsFilled, r.budgetDecision, requisitionStatusLabel(r.status), r.reasonCode, isoDate(r.createdAtUtc)]));
    notify('Requisitions CSV exported.');
  };

  const stageList = chosen ? stages[chosen.type] : [];
  const reached = chosen ? stageList.indexOf(chosen.status) : -1;
  const waited = chosen ? -daysFromToday(chosen.createdAtUtc) : 0;

  return <><Header page="requisitions" actions={<><Button disabled={!rows.length} onClick={exportCsv}>Export</Button><Button variant="primary" disabled={!departments.data} onClick={()=>setRaising(true)}>Raise requisition</Button></>}/>
    <div className="content"><div className="grid stats"><Stat label="Open" value={all.data?open.length:'…'} note={`${open.filter(r=>r.type==='Hire').length} hire · ${open.filter(r=>r.type==='Termination').length} termination`}/><Stat label="Awaiting Budget" value={all.data?awaiting.length:'…'} note={awaiting.length?`Oldest waiting ${oldest} days`:'Nothing waiting'}/><Stat label="Approved, unfilled" value={all.data?unfilled.length:'…'} note={`${unfilled.reduce((n,r)=>n+r.positions-r.positionsFilled,0)} positions remaining`}/><Stat label="Closed" value={all.data?everything.filter(r=>['Completed','Closed','Rejected'].includes(r.status)).length:'…'} note="Completed, closed or rejected"/></div>
      <ApiNotice error={list.error} retry={list.reload}/>
      <div className="split"><Panel title="All requisitions" action={<div className="row"><Select label="Filter by type" value={type} onChange={setType} options={[['','Type: All'],'Hire','Termination']}/><Select label="Filter by status" value={status} onChange={setStatus} options={[['open','Status: Open'],['','Status: All'],...requisitionStatuses.map(s=>[s,'Status: '+requisitionStatusLabel(s)])]}/><Select label="Filter by department" value={dept} onChange={setDept} options={[['','Department: All'],...(departments.data??[]).map(d=>[d.code,'Department: '+d.name])]}/></div>} footer={<span className="tiny">Showing {rows.length} requisitions · raised by peer services and this screen</span>}>
        <div className="table-wrap"><table className="tbl"><thead><tr>{['Requisition','Type','Requesting dept','Role','Posts','Budget','Status','Raised','Select'].map(h=><th key={h}>{h}</th>)}</tr></thead><tbody>{rows.map(r=><tr key={r.requisitionNumber} className={chosen?.requisitionNumber===r.requisitionNumber?'selected':''}><td className="mono nowrap">{r.requisitionNumber}</td><td><Pill kind={r.type==='Hire'?'info':'clay'}>{r.type}</Pill></td><td>{r.department.name}</td><td>{r.roleTitle}{r.staffNumber&&<div className="tiny mono">{r.staffNumber}</div>}</td><td className="mono">{r.type==='Hire'?`${r.positionsFilled}/${r.positions}`:'1'}</td><td>{r.budgetDecision?<Pill>{r.budgetDecision}</Pill>:<span className="muted">—</span>}</td><td><Pill>{requisitionStatusLabel(r.status)}</Pill></td><td className="nowrap mono">{isoDate(r.createdAtUtc)}</td><td><Button small onClick={()=>setSelected(r.requisitionNumber)}>View</Button></td></tr>)}</tbody></table>{!rows.length&&<Empty loading={list.loading}>No requisitions match these filters.</Empty>}</div>
      </Panel>
      {chosen&&<aside className="rail"><Panel title={<><span className="eyebrow">Selected requisition</span><br/><span className="mono">{chosen.requisitionNumber}</span></>} action={<Pill kind={chosen.type==='Hire'?'info':'clay'}>{chosen.type}</Pill>} footer={actions(chosen).length?<div className="row">{actions(chosen).map(([label,body,variant])=><Button key={label} small variant={variant} disabled={busy} onClick={()=>move(chosen,body)}>{label}</Button>)}{chosen.type==='Hire'&&['Approved','Filling'].includes(chosen.status)&&chosen.positionsFilled<chosen.positions&&<Button small onClick={()=>go('onboard')}>Onboard a hire</Button>}</div>:null}>
        <div className="card-body"><dl className="kv"><dt>Requested by</dt><dd>{chosen.department.name}</dd><dt>{chosen.type==='Hire'?'Role':'Employee'}</dt><dd>{chosen.type==='Hire'?chosen.roleTitle:<button className="link mono" onClick={()=>go('profile',{staffNumber:chosen.staffNumber})}>{chosen.staffNumber}</button>}</dd>{chosen.type==='Hire'&&<><dt>Positions</dt><dd>{chosen.positionsFilled} of {chosen.positions} filled</dd><dt>Budget</dt><dd>{chosen.budgetDecision?humanize(chosen.budgetDecision):'Not yet asked'}</dd></>}<dt>Raised</dt><dd className="mono">{isoDate(chosen.createdAtUtc)}</dd><dt>Reason</dt><dd className="mono">{chosen.reasonCode||'—'}</dd></dl><hr className="rule"/><span className="eyebrow">Progress</span>{stageList.map((step,i)=><div className={'step '+(i<reached||chosen.status==='Completed'?'done':i===reached?'current':'')} key={step}><span className="step-dot">{i<reached||chosen.status==='Completed'?'✓':i===reached?'•':''}</span><div>{requisitionStatusLabel(step)}<span className="tiny">{i===reached&&chosen.status!=='Completed'?'Current stage':i<reached||chosen.status==='Completed'?'Done':''}</span></div></div>)}
          {['Rejected','Closed'].includes(chosen.status)&&<div className="note bad">This requisition is {chosen.status.toLowerCase()}; it can't move any further.</div>}
          {chosen.status==='AwaitingBudget'&&waited>BUDGET_SLA_DAYS&&<div className="note warn">Budget’s funding decision SLA is {BUDGET_SLA_DAYS} days. This requisition has waited {waited} days.</div>}
          {chosen.type==='Termination'&&chosen.status==='Approved'&&<div className="note warn">Completing it terminates {chosen.staffNumber}: assignments end, future shifts are removed and Staff.Terminated is published.</div>}
        </div>
      </Panel></aside>}</div>
    </div>
    {raising&&<RaiseDialog departments={departments.data??[]} close={()=>setRaising(false)} done={(number)=>{setRaising(false);setStatus('open');setSelected(number);reload();}} notify={notify}/>}
  </>;
}
