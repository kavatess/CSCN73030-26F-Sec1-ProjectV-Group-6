import { useEffect, useState } from 'react';
import { initialStaff, initialRequisitions, initialCredentials } from './demoData';
import Overview from './pages/Overview';
import Directory from './pages/Directory';
import Roster from './pages/Roster';
import Requisitions from './pages/Requisitions';
import Credentials from './pages/Credentials';
import Profile from './pages/Profile';
import Onboard from './pages/Onboard';
import './App.css';

const menu = [['overview','Overview'],['directory','Staff directory'],['roster','Duty roster'],['requisitions','Requisitions','18'],['credentials','Credentials','23']];

const currentPage=()=>['overview','directory','roster','requisitions','credentials','profile','onboard'].includes(location.hash.slice(1))?location.hash.slice(1):'overview';
export default function App() {
  const [page,setPage]=useState(currentPage),[context,setContext]=useState({}),[staff,setStaff]=useState(initialStaff),[requisitions,setRequisitions]=useState(initialRequisitions),[credentials,setCredentials]=useState(initialCredentials),[message,setMessage]=useState('');
  useEffect(()=>{const handle=()=>setPage(currentPage());window.addEventListener('hashchange',handle);return ()=>window.removeEventListener('hashchange',handle);},[]);
  useEffect(()=>{if(!message)return;const timer=setTimeout(()=>setMessage(''),3500);return ()=>clearTimeout(timer);},[message]);
  const go=(next,data={})=>{setContext(data);setPage(next);location.hash=next;window.scrollTo({top:0,behavior:'instant'});};
  const active=page==='profile'||page==='onboard'?'directory':page;
  return <div className="app"><aside className="side"><a className="brand" href="#overview" onClick={e=>{e.preventDefault();go('overview');}}><span className="brand-mark">HR</span><span className="brand-text"><strong>CMES</strong><span>Staff & HR</span></span></a>
    <nav className="nav" aria-label="Main">{menu.map(([key,label,count])=><a key={key} href={'#'+key} aria-current={active===key?'page':undefined} onClick={e=>{e.preventDefault();go(key);}}><span>{label}</span>{count&&<span className={'badge '+(key==='credentials'?'warn':'')}>{count}</span>}</a>)}</nav>
    <div className="side-bottom"><span className="eyebrow">Demo · March 2026 snapshot</span><div className="who"><span className="avatar">MA</span><div><strong>M. Arseneau</strong><span>HR Admin · HRS</span></div></div></div>
  </aside><main className="main">
    {page==='overview'&&<Overview go={go} notify={setMessage}/>}
    {page==='directory'&&<Directory key={JSON.stringify(context)} go={go} staff={staff} filter={context} notify={setMessage}/>}
    {page==='roster'&&<Roster go={go} notify={setMessage}/>}
    {page==='requisitions'&&<Requisitions requisitions={requisitions} setRequisitions={setRequisitions} notify={setMessage}/>}
    {page==='credentials'&&<Credentials credentials={credentials} setCredentials={setCredentials} go={go} notify={setMessage}/>}
    {page==='profile'&&<Profile key={context.person?.id} person={context.person} credentials={credentials} go={go} notify={setMessage}/>}
    {page==='onboard'&&<Onboard go={go} staff={staff} setStaff={setStaff} notify={setMessage}/>}
  </main>{message&&<div className="toast" role="status" aria-live="polite">{message}</div>}</div>;
}
