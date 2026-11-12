import { useState } from 'react';
import { departments } from '../demoData';
import { Pill, Button, Panel, Select, Field, Header, Avatar } from '../ui';
import { csvDownload } from '../screens';

export default function Directory({ go, staff, filter, notify }) {
  const [draft,setDraft]=useState({search:filter.search||'',department:filter.department||'All departments',service:'All services',credential:'Any'});
  const [applied,setApplied]=useState(draft);
  const [status,setStatus]=useState('All');
  const [sort,setSort]=useState('Sort: Last name A–Z');
  const update=(key,value)=>setDraft({...draft,[key]:value});
  let rows=staff.filter(p=>(!applied.search||[p.name,p.id,p.role,p.service].some(x=>x.toLowerCase().includes(applied.search.toLowerCase())))&&(applied.department==='All departments'||p.department===applied.department)&&(applied.service==='All services'||p.service===applied.service)&&(applied.credential==='Any'||(applied.credential==='Expiring ≤ 30 days'?p.credential.includes('Expires'):p.credential===applied.credential))&&(status==='All'||p.status===status));
  rows=[...rows].sort((a,b)=>sort==='Sort: Staff ID'?a.id.localeCompare(b.id):a.name.localeCompare(b.name));if(sort==='Sort: Hire date, newest')rows.reverse();
  return <><Header page="directory" actions={<><Button onClick={()=>notify('Saved view: All active')}>Saved view: All active</Button><Button variant="primary" onClick={()=>go('onboard')}>Onboard employee</Button></>}/>
    <div className="content"><div className="card card-body"><form className="filters" onSubmit={(e)=>{e.preventDefault();setApplied(draft);}}>
      <Field label="Search"><input className="inp" placeholder="Name, staff ID, or email..." value={draft.search} onChange={e=>update('search',e.target.value)}/></Field>
      <Field label="Department"><Select label="Department" value={draft.department} onChange={v=>update('department',v)} options={['All departments',...departments.map(d=>d.name)]}/></Field>
      <Field label="Service"><Select label="Service" value={draft.service} onChange={v=>update('service',v)} options={['All services','Paramedic Service','Fire Suppression','Patrol Division','Bus Operations','Electrical Works','Water & Lines','HR Operations']}/></Field>
      <Field label="Credential state"><Select label="Credential state" value={draft.credential} onChange={v=>update('credential',v)} options={['Any','Valid','Expiring ≤ 30 days','Expired','Suspended','Flagged']}/></Field><Button type="submit" variant="primary">Apply</Button>
    </form><div className="filter-extra"><span className="eyebrow">Status</span>{[['All','1,284'],['Active','1,196'],['Probation','41'],['On leave','38'],['Suspended','9']].map(([s,n])=><button key={s} className={'chip '+(status===s?'active':'')} onClick={()=>setStatus(s)}>{s} <span className="mono">{n}</span></button>)}<span className="right-group tiny">Sample records · March 2026</span></div></div>
      <Panel title={status==='All'?'All staff — 1,284 results':status+' staff — '+({Active:'1,196',Probation:'41','On leave':'38',Suspended:'9'}[status])+' results'} action={<div className="row"><Select label="Sort by" value={sort} onChange={setSort} options={['Sort: Last name A–Z','Sort: Hire date, newest','Sort: Staff ID']}/><Button small onClick={()=>{csvDownload('cmes-staff-sample.csv',['Staff ID','Name','Department','Service','Role','Status','Credential','Duty today'],rows.map(p=>[p.id,p.name,p.department,p.service,p.role,p.status,p.credential,p.duty]));notify('Filtered sample exported.');}}>Export</Button></div>} footer={<span className="tiny">Showing {rows.length} sample records · filters apply to sample data</span>}>
        <div className="table-wrap"><table className="tbl"><thead><tr>{['Staff ID','Name','Department','Service · Role','Employment','Credentials','Duty today','Actions'].map(x=><th key={x}>{x}</th>)}</tr></thead><tbody>{rows.map(p=><tr key={p.id}><td className="mono nowrap">{p.id}</td><td><div className="person"><Avatar name={p.name}/><button className="link" onClick={()=>go('profile',{person:p})}>{p.name}</button></div></td><td>{p.department}</td><td>{p.service} · {p.role}</td><td><Pill>{p.status}</Pill></td><td><Pill>{p.credential}</Pill></td><td className="mono nowrap">{p.duty}</td><td><Button small onClick={()=>go('profile',{person:p})}>Open</Button></td></tr>)}</tbody></table>{!rows.length&&<div className="empty-state">No sample staff match these filters.</div>}</div>
      </Panel><p className="tiny">Salary is never returned by this endpoint. Compensation is shared with Budget only as a department total.</p>
    </div>
  </>;
}
