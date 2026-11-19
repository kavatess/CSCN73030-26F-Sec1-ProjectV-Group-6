import { useEffect, useState } from 'react';
import Overview from './pages/Overview';
import Directory from './pages/Directory';
import Roster from './pages/Roster';
import Requisitions from './pages/Requisitions';
import Credentials from './pages/Credentials';
import Profile from './pages/Profile';
import Onboard from './pages/Onboard';
import { api, openRequisitionStatuses } from './api';
import { useApi } from './useApi';
import './App.css';

const menu = [['overview','Overview'],['directory','Staff directory'],['roster','Duty roster'],['requisitions','Requisitions'],['credentials','Credentials']];

const pages=['overview','directory','roster','requisitions','credentials','profile','onboard'];
/** "#profile/STF-0000002" → { page:'profile', id:'STF-0000002' }. Unknown hashes open the overview. */
const parseHash=(hash)=>{const [name,id='']=hash.replace(/^#/,'').split('/');return pages.includes(name)?{page:name,id:decodeURIComponent(id)}:{page:'overview',id:''};};
export default function App() {
  const [hash,setHash]=useState(()=>location.hash),[context,setContext]=useState({}),[message,setMessage]=useState('');
  const {page,id}=parseHash(hash);
  useEffect(()=>{const handle=()=>setHash(location.hash);window.addEventListener('hashchange',handle);return ()=>window.removeEventListener('hashchange',handle);},[]);
  useEffect(()=>{if(!message)return;const timer=setTimeout(()=>setMessage(''),3500);return ()=>clearTimeout(timer);},[message]);
  // Sidebar badges: open requisitions and credentials expiring within 30 days. API status at the bottom.
  const openRequisitions=useApi(()=>api.requisitions(),[page]),expiring=useApi(()=>api.expiringCredentials(30),[page]),health=useApi(()=>api.health(),[]);
  const counts={requisitions:openRequisitions.data?.filter(r=>openRequisitionStatuses.includes(r.status)).length||null,credentials:expiring.data?.length||null};
  const apiUp=health.data?.status==='Healthy';
  const go=(next,data={})=>{const staffNumber=data.staffNumber||data.person?.id;const target='#'+next+(next==='profile'&&staffNumber?'/'+encodeURIComponent(staffNumber):'');setContext(data);setHash(target);location.hash=target;window.scrollTo({top:0,behavior:'instant'});};
  const active=page==='profile'||page==='onboard'?'directory':page;
  return <div className="app"><aside className="side"><a className="brand" href="#overview" onClick={e=>{e.preventDefault();go('overview');}}><span className="brand-mark">HR</span><span className="brand-text"><strong>CMES</strong><span>Staff & HR</span></span></a>
    <nav className="nav" aria-label="Main">{menu.map(([key,label])=><a key={key} href={'#'+key} aria-current={active===key?'page':undefined} onClick={e=>{e.preventDefault();go(key);}}><span>{label}</span>{counts[key]&&<span className={'badge '+(key==='credentials'?'warn':'')}>{counts[key]}</span>}</a>)}</nav>
    <div className="side-bottom"><span className="eyebrow">Staff/HR API</span><div className="who" role="status" aria-label="API status"><span className="avatar">{apiUp?'✓':'!'}</span><div><strong>{health.loading?'Connecting…':apiUp?'Connected':health.data?'Degraded':'Unreachable'}</strong><span>{health.data?'Database '+(health.data.checks?.database??'unknown').toLowerCase():health.error?'Start the ASP.NET server':''}</span></div></div></div>
  </aside><main className="main">
    {page==='overview'&&<Overview go={go} notify={setMessage}/>}
    {page==='directory'&&<Directory key={JSON.stringify(context)} go={go} filter={context} notify={setMessage}/>}
    {page==='roster'&&<Roster go={go} notify={setMessage}/>}
    {page==='requisitions'&&<Requisitions key={JSON.stringify(context)} go={go} filter={context} notify={setMessage}/>}
    {page==='credentials'&&<Credentials key={JSON.stringify(context)} go={go} filter={context} notify={setMessage}/>}
    {page==='profile'&&<Profile key={id} staffNumber={id} go={go} notify={setMessage}/>}
    {page==='onboard'&&<Onboard go={go} notify={setMessage}/>}
  </main>{message&&<div className="toast" role="status" aria-live="polite">{message}</div>}</div>;
}
