import { useState } from 'react';
import { api, credentialStates, employmentStatuses, employmentTypeLabel, formatCount, humanize, sortName } from '../api';
import { useApi } from '../useApi';
import { Pill, Button, Panel, Select, Field, Header, Avatar, ApiNotice, Empty } from '../ui';
import { csvDownload } from '../screens';

const PAGE_SIZE = 20;
const blank = { search: '', department: '', service: '', credential: '' };

/** The query for GET /staff from the applied filters (status chips are added separately). */
const toQuery = (f) => ({ q: f.search.trim(), department: f.department, service: f.service, credentialState: f.credential });

export default function Directory({ go, filter, notify }) {
  const initial = { ...blank, search: filter.search || '', department: filter.department || '' };
  const [draft, setDraft] = useState(initial);
  const [applied, setApplied] = useState(initial);
  const [status, setStatus] = useState(filter.status || '');
  const [sort, setSort] = useState('name');
  const [page, setPage] = useState(1);
  const update = (key, value) => setDraft((d) => ({ ...d, [key]: value, ...(key === 'department' ? { service: '' } : {}) }));

  const departments = useApi(() => api.departments(), []);
  const services = useApi(() => (draft.department ? api.services(draft.department) : Promise.resolve([])), [draft.department]);
  const result = useApi(() => api.searchStaff({ ...toQuery(applied), status, page, pageSize: PAGE_SIZE }), [applied, status, page]);
  // One count per status chip, for the same filters.
  const counts = useApi(async () => {
    const totals = await Promise.all(['', ...employmentStatuses].map((s) => api.searchStaff({ ...toQuery(applied), status: s, pageSize: 1 }).then((r) => r.total)));
    return Object.fromEntries(['', ...employmentStatuses].map((s, i) => [s, totals[i]]));
  }, [applied]);
  const onDuty = useApi(() => api.onDuty(), []);
  const onDutyNow = new Set((onDuty.data ?? []).map((e) => e.staffNumber));

  const total = result.data?.total ?? 0;
  const rows = [...(result.data?.items ?? [])].sort((a, b) => (sort === 'id' ? a.staffNumber.localeCompare(b.staffNumber) : sortName(a).localeCompare(sortName(b))));
  const first = total ? (page - 1) * PAGE_SIZE + 1 : 0;
  const last = Math.min(page * PAGE_SIZE, total);
  const chooseStatus = (s) => { setStatus(s); setPage(1); };

  const exportCsv = async () => {
    try {
      const all = [];
      for (let p = 1; ; p++) {
        const chunk = await api.searchStaff({ ...toQuery(applied), status, page: p, pageSize: 100 });
        all.push(...chunk.items);
        if (all.length >= chunk.total || !chunk.items.length) break;
      }
      csvDownload('cmes-staff.csv', ['Staff ID', 'Name', 'Email', 'Department', 'Service', 'Role', 'Employment status', 'Employment type', 'Credentials'],
        all.map((p) => [p.staffNumber, sortName(p), p.email, p.homeDepartment.name, p.currentService, p.currentRole, humanize(p.employmentStatus), employmentTypeLabel(p.employmentType), humanize(p.credentialState)]));
      notify(`Exported ${all.length} staff records.`);
    } catch (error) {
      notify(error.message);
    }
  };

  return <><Header page="directory" route={'GET /STAFF' + (status ? '?STATUS=' + status.toUpperCase() : '')} actions={<><Button onClick={()=>{setDraft(blank);setApplied(blank);chooseStatus('Active');}}>Saved view: All active</Button><Button variant="primary" onClick={()=>go('onboard')}>Onboard employee</Button></>}/>
    <div className="content"><div className="card card-body"><form className="filters" onSubmit={(e)=>{e.preventDefault();setApplied(draft);setPage(1);}}>
      <Field label="Search"><input className="inp" placeholder="Name, staff ID, or email..." value={draft.search} onChange={e=>update('search',e.target.value)}/></Field>
      <Field label="Department"><Select label="Department" value={draft.department} onChange={v=>update('department',v)} options={[['','All departments'],...(departments.data??[]).map(d=>[d.code,d.name])]}/></Field>
      <Field label="Service"><Select label="Service" value={draft.service} disabled={!draft.department} onChange={v=>update('service',v)} options={[['',draft.department?'All services':'Choose a department'],...(services.data??[]).map(s=>[String(s.serviceId),s.name])]}/></Field>
      <Field label="Credential state"><Select label="Credential state" value={draft.credential} onChange={v=>update('credential',v)} options={[['','Any'],...credentialStates.map(s=>[s,humanize(s)])]}/></Field><Button type="submit" variant="primary">Apply</Button>
    </form><div className="filter-extra"><span className="eyebrow">Status</span>{['',...employmentStatuses].map(s=><button key={s||'all'} className={'chip '+(status===s?'active':'')} onClick={()=>chooseStatus(s)}>{s?humanize(s):'All'} <span className="mono">{counts.data?formatCount(counts.data[s]):'…'}</span></button>)}<span className="right-group tiny">Live data · <span className="mono">GET /staff</span></span></div></div>
      <ApiNotice error={result.error} retry={result.reload}/>
      <Panel title={(status?humanize(status)+' staff':'All staff')+' — '+formatCount(total)+' results'} action={<div className="row"><Select label="Sort by" value={sort} onChange={setSort} options={[['name','Sort: Last name A–Z'],['id','Sort: Staff ID']]}/><Button small disabled={!total} onClick={exportCsv}>Export</Button></div>} footer={<><span className="tiny">{total?`Showing ${first}–${last} of ${formatCount(total)}`:'No results'}{result.loading&&' · loading…'}</span><div className="row"><Button small disabled={page<=1} onClick={()=>setPage(page-1)}>‹ Previous</Button><Button small disabled={last>=total} onClick={()=>setPage(page+1)}>Next ›</Button></div></>}>
        <div className="table-wrap"><table className="tbl"><thead><tr>{['Staff ID','Name','Department','Service · Role','Employment','Credentials','Duty now','Actions'].map(x=><th key={x}>{x}</th>)}</tr></thead><tbody>{rows.map(p=><tr key={p.staffNumber}><td className="mono nowrap">{p.staffNumber}</td><td><div className="person"><Avatar name={sortName(p)}/><button className="link" onClick={()=>go('profile',{staffNumber:p.staffNumber})}>{sortName(p)}</button></div></td><td>{p.homeDepartment.name}</td><td>{p.currentService?`${p.currentService} · ${p.currentRole}`:<span className="muted">No current assignment</span>}</td><td><Pill>{humanize(p.employmentStatus)}</Pill></td><td>{p.credentialState?<Pill>{humanize(p.credentialState)}</Pill>:<span className="muted">None held</span>}</td><td className="mono nowrap">{onDutyNow.has(p.staffNumber)?<Pill kind="ok">On duty</Pill>:'—'}</td><td><Button small onClick={()=>go('profile',{staffNumber:p.staffNumber})}>Open</Button></td></tr>)}</tbody></table>{!rows.length&&<Empty loading={result.loading}>No staff match these filters.</Empty>}</div>
      </Panel><p className="tiny">Salary is never returned by this endpoint. Compensation is shared with Budget only as a department total.</p>
    </div>
  </>;
}
