import { useState } from 'react';
import { api, credentialStates, formatCount, humanize, isoDate, localDate, remainingText } from '../api';
import { useApi } from '../useApi';
import { Pill, Button, Panel, Select, Field, Header, Avatar, Dialog, ApiNotice, Empty } from '../ui';
import { csvDownload } from '../screens';

const PAGE_SIZE = 20;

/** Every credential, page by page (the side panels summarise all of them). */
async function allCredentials() {
  const all = [];
  for (let page = 1; ; page++) {
    const chunk = await api.credentials({ page, pageSize: 100 });
    all.push(...chunk.items);
    if (all.length >= chunk.total || !chunk.items.length) return all;
  }
}

function RenewDialog({ credential, close, done, notify }) {
  const [form, setForm] = useState({ number: credential.number, issueDate: localDate(), expiryDate: '' });
  const [errors, setErrors] = useState({}), [busy, setBusy] = useState(false);
  const set = (k, v) => setForm(f => ({ ...f, [k]: v }));
  const submit = async (e) => {
    e.preventDefault(); setBusy(true); setErrors({});
    try {
      const updated = await api.updateCredential(credential.credentialId, form);
      notify(`${credential.credentialType} for ${credential.staffName} renewed — now ${humanize(updated.status).toLowerCase()}.`);
      done();
    } catch (error) {
      setErrors(Object.fromEntries(Object.entries(error.errors).map(([k, v]) => [k, v.join(' ')])));
      if (!Object.keys(error.errors).length) notify(error.message);
      setBusy(false);
    }
  };
  return <Dialog title={'Record renewal · ' + credential.staffName} action="Record renewal" busy={busy} close={close} submit={submit}>
    <div className="note info">{credential.credentialType} · current expiry {isoDate(credential.expiryDate)}{credential.status === 'Suspended' ? ' · suspended (renewing does not lift a suspension)' : ''}</div>
    <Field label="Credential number" error={errors.number}><input required className="inp" aria-label="Credential number" value={form.number} onChange={e=>set('number',e.target.value)}/></Field>
    <div className="form-grid"><Field label="Issued" error={errors.issueDate}><input required className="inp" aria-label="Issued" type="date" value={form.issueDate} onChange={e=>set('issueDate',e.target.value)}/></Field>
      <Field label="New expiry" error={errors.expiryDate}><input required className="inp" aria-label="New expiry" type="date" value={form.expiryDate} onChange={e=>set('expiryDate',e.target.value)}/></Field></div>
  </Dialog>;
}

export default function Credentials({ go, notify, filter = {} }) {
  const [state, setState] = useState(filter.state || ''), [dept, setDept] = useState(''), [type, setType] = useState(''), [page, setPage] = useState(1), [renewing, setRenewing] = useState(null), [busy, setBusy] = useState(null);
  const departments = useApi(() => api.departments(), []);
  const types = useApi(() => api.credentialTypes(), []);
  const list = useApi(() => (state === 'Expiring90'
    ? api.expiringCredentials(90).then(items => ({ items: items.filter(c => (!dept || c.departmentCode === dept) && (!type || String(c.credentialTypeId) === type)), total: items.length }))
    : api.credentials({ state, department: dept, type, page, pageSize: PAGE_SIZE })), [state, dept, type, page]);
  const all = useApi(allCredentials, []);
  const everything = all.data ?? [];
  const count = (s) => everything.filter(c => c.status === s).length;
  const within90 = everything.filter(c => c.status !== 'Suspended' && c.daysUntilExpiry >= 0 && c.daysUntilExpiry <= 90).length;
  const blocking = everything.filter(c => c.isDutyBlocking && ['Expired', 'Suspended'].includes(c.status));
  const rows = list.data?.items ?? [];
  const total = state === 'Expiring90' ? rows.length : list.data?.total ?? 0;
  const paged = state !== 'Expiring90';
  const choose = (setter) => (value) => { setter(value); setPage(1); };
  const reload = () => { list.reload(); all.reload(); };

  const toggleSuspension = async (c) => {
    const suspend = c.status !== 'Suspended';
    if (suspend && !window.confirm(`Suspend ${c.credentialType} for ${c.staffName}?${c.isDutyBlocking ? ' They will fail the duty eligibility check.' : ''}`)) return;
    setBusy(c.credentialId);
    try { await api.updateCredential(c.credentialId, { suspended: suspend }); notify(`${c.credentialType} ${suspend ? 'suspended' : 'reinstated'} for ${c.staffName}.`); reload(); }
    catch (error) { notify(error.message); }
    setBusy(null);
  };
  const exportCsv = () => {
    csvDownload('cmes-credentials.csv', ['Staff ID', 'Employee', 'Department', 'Credential', 'Number', 'Issued', 'Expires', 'Days left', 'Duty-blocking', 'State'],
      everything.map(c => [c.staffNumber, c.staffName, c.departmentCode, c.credentialType, c.number, c.issueDate, c.expiryDate, c.daysUntilExpiry, c.isDutyBlocking ? 'Yes' : 'No', c.status]));
    notify('Credentials CSV exported.');
  };
  const mostUrgent = everything.find(c => c.status !== 'Valid' && c.status !== 'Suspended');
  const chips = [['', 'All', everything.length], ...credentialStates.map(s => [s, humanize(s), count(s)]), ['Expiring90', 'Expiring ≤ 90d', within90]];

  return <><Header page="credentials" route={'GET /CREDENTIALS' + (state && state !== 'Expiring90' ? '?STATE=' + state.toUpperCase() : state ? '/EXPIRING?WITHINDAYS=90' : '')} actions={<><Button disabled={!everything.length} onClick={exportCsv}>Export</Button><Button variant="primary" disabled={!mostUrgent} onClick={()=>setRenewing(mostUrgent)}>Record renewal</Button></>}/>
    <div className="content">
      <div className="card card-body filter-extra" style={{margin:0,borderTop:0,paddingTop:14}}><span className="eyebrow">State</span>{chips.map(([value,label,n])=><button key={value||'all'} className={'chip '+(state===value?'active':'')} onClick={()=>choose(setState)(value)}>{label} <span className="mono">{all.data?formatCount(n):'…'}</span></button>)}<div className="right-group row"><Select label="Credential department" value={dept} onChange={choose(setDept)} options={[['','Department: All'],...(departments.data??[]).map(d=>[d.code,'Department: '+d.name])]}/><Select label="Credential type" value={type} onChange={choose(setType)} options={[['','Type: All'],...(types.data??[]).map(t=>[String(t.credentialTypeId),'Type: '+t.name])]}/></div></div>
      <ApiNotice error={list.error} retry={list.reload}/>
      <div className="split"><Panel title={(chips.find(c=>c[0]===state)?.[1]??'All')+' credentials — '+formatCount(total)+' records'} action={<span className="tiny">Most urgent first</span>} footer={<><span className="tiny">{paged&&total?`Showing ${(page-1)*PAGE_SIZE+1}–${Math.min(page*PAGE_SIZE,total)} of ${formatCount(total)}`:`${rows.length} records`}{list.loading&&' · loading…'}</span>{paged&&<div className="row"><Button small disabled={page<=1} onClick={()=>setPage(page-1)}>‹ Previous</Button><Button small disabled={page*PAGE_SIZE>=total} onClick={()=>setPage(page+1)}>Next ›</Button></div>}</>}>
        <div className="table-wrap"><table className="tbl"><thead><tr>{['Employee','Credential','Number','Expires','Remaining','State','Actions'].map(h=><th key={h}>{h}</th>)}</tr></thead><tbody>{rows.map(c=><tr key={c.credentialId}><td><div className="person"><Avatar name={c.staffName}/><div><button className="link" onClick={()=>go('profile',{staffNumber:c.staffNumber})}>{c.staffName}</button><div className="tiny mono nowrap">{c.staffNumber}</div></div></div></td><td>{c.credentialType}<div className="tiny">{c.departmentCode} · {c.isDutyBlocking?'duty-blocking':'advisory'}</div></td><td className="mono nowrap">{c.number}</td><td className="mono nowrap">{isoDate(c.expiryDate)}</td><td className="nowrap">{remainingText(c.daysUntilExpiry)}</td><td><Pill>{humanize(c.status)}</Pill></td><td><div className="row" style={{flexWrap:'nowrap'}}><Button small onClick={()=>setRenewing(c)}>Renew</Button><Button small variant={c.status==='Suspended'?'':'danger'} disabled={busy===c.credentialId} onClick={()=>toggleSuspension(c)}>{c.status==='Suspended'?'Reinstate':'Suspend'}</Button></div></td></tr>)}</tbody></table>{!rows.length&&<Empty loading={list.loading}>No credentials match these filters.</Empty>}</div>
      </Panel><aside className="rail">
        <Panel title="Blocking duty right now" action={<Pill kind={blocking.length?'bad':'ok'}>{blocking.length}</Pill>}>{blocking.map(c=><div className="list-row" key={c.credentialId}><button className="link" onClick={()=>go('profile',{staffNumber:c.staffNumber})}><strong>{c.staffName}</strong></button><div className="tiny">{c.credentialType} {c.status==='Suspended'?'suspended':'expired '+isoDate(c.expiryDate)} — fails the eligibility check</div></div>)}{!blocking.length&&<div className="list-row tiny">{all.loading?'Loading…':'No duty-blocking credential is expired or suspended.'}</div>}<div className="notice-end">Eligibility checks return false while a duty-blocking credential is expired or suspended.</div></Panel>
        <Panel title="By credential type"><div className="table-wrap"><table className="tbl"><thead><tr><th>Type</th><th>Held</th><th>≤ 30d</th></tr></thead><tbody>{(types.data??[]).map(t=>{const held=everything.filter(c=>c.credentialTypeId===t.credentialTypeId);return held.length?<tr key={t.credentialTypeId}><td>{t.name}</td><td className="mono">{held.length}</td><td className="mono amber">{held.filter(c=>c.status==='Expiring'||(c.daysUntilExpiry>=0&&c.daysUntilExpiry<=30&&c.status!=='Suspended')).length}</td></tr>:null;})}</tbody></table></div></Panel>
        <Panel title="Status rules">{[['Valid','More than 30 days left'],['Expiring','30 days or fewer'],['Expired','Past the expiry date'],['Suspended','Set by HR; overrides the dates']].map(([n,d])=><div className="side-panel-row" key={n}><strong>{n}</strong><span className="tiny">{d}</span></div>)}</Panel>
      </aside></div>
    </div>
    {renewing&&<RenewDialog credential={renewing} close={()=>setRenewing(null)} done={()=>{setRenewing(null);reload();}} notify={notify}/>}
  </>;
}
