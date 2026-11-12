/**
 * @file ui.jsx
 * @brief Shared building blocks of the Staff & HR screens (pills, panels, dialogs, headers).
 */
import { useEffect } from 'react';
import { labels, routes } from './screens';

const tone = (s) => /expired|suspended|flagged|critical|rejected|short|not eligible/i.test(s) ? 'bad' : /pending|await|expir|probation|leave|draft|review|stalled/i.test(s) ? 'warn' : /valid|active|approved|completed|published|clear|eligible|hire|yes/i.test(s) ? 'ok' : 'info';
export const Pill = ({ children, kind }) => <span className={'pill ' + (kind || tone(String(children)))}>{children}</span>;
export const Button = ({ children, onClick, variant='', small=false, disabled=false, type='button' }) => <button type={type} disabled={disabled} className={'btn ' + variant + (small?' sm':'')} onClick={onClick}>{children}</button>;
export const Stat = ({ label, value, note }) => <div className="stat"><span className="eyebrow">{label}</span><span className="stat-value">{value}</span><span className="stat-note">{note}</span></div>;
export const Panel = ({ title, action, footer, children }) => <section className="card"><div className="card-head"><h2>{title}</h2>{action}</div>{children}{footer && <div className="card-foot">{footer}</div>}</section>;
export const Select = ({ label, value, onChange, options }) => <select className="sel" aria-label={label} value={value} onChange={(e)=>onChange(e.target.value)}>{options.map(x=><option key={x}>{x}</option>)}</select>;
export const Field = ({ label, children, hint, error }) => <label className="field"><span>{label}</span>{hint && <small>{hint}</small>}{children}{error && <span className="error">{error}</span>}</label>;
export const Header = ({ page, title, actions }) => <header className="topbar"><div className="topbar-title"><span className="eyebrow">{routes[page]}</span><h1>{title||labels[page]}</h1></div><div className="topbar-actions">{actions}</div></header>;
export const Avatar = ({ name }) => <span className="person-avatar">{(name.includes(', ') ? name.split(', ').reverse() : name.split(' ')).slice(0,2).map(x=>x[0]).join('').toUpperCase()}</span>;
export function Dialog({ title, children, close, submit, action='Save' }) {
  useEffect(()=>{ const onKey=(e)=>{if(e.key==='Escape')close();}; document.addEventListener('keydown',onKey); return ()=>document.removeEventListener('keydown',onKey); },[close]);
  return <div className="modal-backdrop" onMouseDown={(e)=>{if(e.target===e.currentTarget)close();}}><form className="modal" onSubmit={submit} aria-label={title}><div className="card-head"><h2>{title}</h2><Button onClick={close} small>✕</Button></div><div className="card-body stack">{children}</div><div className="card-foot"><Button onClick={close}>Cancel</Button><Button type="submit" variant="primary">{action}</Button></div></form></div>;
}
