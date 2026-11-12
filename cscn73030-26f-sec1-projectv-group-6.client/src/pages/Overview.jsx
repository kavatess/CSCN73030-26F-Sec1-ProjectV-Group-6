import { useState } from 'react';
import { departments, initialStaff } from '../demoData';
import { Pill, Button, Stat, Panel } from '../ui';
import { csvDownload, routes } from '../screens';

export default function Overview({ go, notify }) {
  const [search,setSearch]=useState('');
  return <>
    <header className="topbar"><div className="topbar-title"><span className="eyebrow">{routes.overview}</span><h1>Workforce overview</h1></div><form className="overview-search" onSubmit={(e)=>{e.preventDefault();if(search.trim())go('directory',{search});}}><input className="inp" aria-label="Search staff" placeholder="Search name or staff ID..." value={search} onChange={(e)=>setSearch(e.target.value)}/><div className="topbar-actions"><Button onClick={()=>go('requisitions')}>Requisitions</Button><Button variant="primary" onClick={()=>go('onboard')}>Onboard employee</Button></div></form></header>
    <div className="content">
      <div className="grid stats"><Stat label="Active staff" value="1,284" note="+12 onboarded this month"/><Stat label="On duty now" value="312" note="Across 6 departments"/><Stat label="Open requisitions" value="18" note={<Pill kind="warn">6 awaiting Budget</Pill>}/><Stat label="Credentials expiring ≤ 30d" value="23" note={<Pill kind="bad">9 block duty eligibility</Pill>}/></div>
      <div className="split">
        <Panel title="Headcount by department" action={<Button small onClick={()=>{csvDownload('cmes-headcount.csv',['Department','Code','Active','On duty','Vacancies','Monthly payroll'],departments.map(d=>[d.name,d.code,d.active,d.duty,d.vacancies,d.payroll]));notify('Headcount CSV exported.');}}>Export CSV</Button>} footer={<><span className="tiny">7 departments · 1,284 active · 29 vacancies</span><span className="tiny mono">Total $9.97M / month</span></>}>
          <div className="card-body" style={{padding:'7px 14px'}}><span className="small muted">Live counts served to peer services on <span className="mono">GET /rosters/headcount</span></span></div>
          <div className="table-wrap"><table className="tbl"><thead><tr><th>Department</th><th>Code</th><th>Active</th><th>On duty</th><th>Vacancies</th><th className="right">Monthly payroll → Budget</th></tr></thead><tbody>{departments.map(d=><tr key={d.code}><td><button className="link" onClick={()=>go('directory',{department:d.name})}>{d.name}</button></td><td className="mono">{d.code}</td><td><div className="row nowrap"><span className="mono" style={{minWidth:38}}>{d.active}</span><div className="bar" style={{width:138}}><span style={{width:Math.max(3,d.active/412*100)+'%'}}/></div></div></td><td className="mono">{d.duty??'—'}</td><td className="mono">{d.vacancies}</td><td className="right mono">{d.payroll}</td></tr>)}</tbody></table></div>
        </Panel>
        <aside className="rail">
          <Panel title="Requires attention" action={<Pill kind="bad">5</Pill>}>{[['Blocks duty','PTR','4 Class D licences expire within 14 days','Open credential tracker →','credentials'],['Security flag','SEC','Clearance suspended for STF-0003118 — removed from patrol roster','Review profile →','profile'],['Stalled','EMS','REQ-2026-0191 has waited 6 days on a Budget decision','Open requisition →','requisitions'],['Under-covered','EMS','Sat 14 Mar — paramedic night shift is 2 short','Open roster →','roster'],['Integration','OUTBOX','3 events unpublished for more than 15 minutes',null,null]].map(([tag,code,copy,link,page])=><div className="list-row" key={tag}><div className="row"><Pill kind={tag==='Blocks duty'||tag==='Security flag'?'bad':'warn'}>{tag}</Pill><span className="eyebrow">{code}</span></div><p>{copy}</p>{link&&<button className="link" onClick={()=>go(page,page==='profile'?{person:initialStaff[1]}:{})}>{link}</button>}</div>)}</Panel>
          <Panel title="Outbound events"><div className="list-row tiny">Azure Service Bus · exactly-once</div>{[['Staff.Onboarded v1','STF-0004217 · 09:42 → SEC, BUD','Published'],['Staff.Terminated v1','STF-0002884 · 09:17 → EMS, SEC, BUD','Published'],['Staff.Onboarded v1','STF-0004216 · 08:55 → PTR','Retrying (2)']].map(([n,d,s],i)=><div className="list-row" key={i}><div className="row space"><span className="mono teal small">{n}</span><Pill kind={s==='Published'?'ok':'warn'}>{s}</Pill></div><span className="tiny">{d}</span></div>)}<div className="notice-end">Retries are idempotent; consumers de-duplicate on event ID.</div></Panel>
        </aside>
      </div>
    </div>
  </>;
}
