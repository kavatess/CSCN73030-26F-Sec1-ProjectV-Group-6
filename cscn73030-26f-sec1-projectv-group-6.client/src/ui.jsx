/**
 * @file ui.jsx
 * @brief Shared building blocks of the Staff & HR screens (pills, panels, dialogs, headers).
 */
import { useEffect } from 'react';
import { labels, routes } from './screens';

/** Pill colour from its words: bad (expired, rejected...), warn (pending, expiring...), ok (valid, approved...) or info. */
const tone = (s) => /expired|suspended|flagged|critical|rejected|short|not eligible/i.test(s) ? 'bad' : /pending|await|expir|probation|leave|draft|review|stalled/i.test(s) ? 'warn' : /valid|active|approved|completed|published|clear|eligible|hire|yes/i.test(s) ? 'ok' : 'info';
/** A small status label; `kind` overrides the colour picked from its text. */
export const Pill = ({ children, kind }) => <span className={'pill ' + (kind || tone(String(children)))}>{children}</span>;
/** A button; `variant` is '', 'primary' or 'danger', `small` makes it compact. */
export const Button = ({ children, onClick, variant='', small=false, disabled=false, type='button' }) => <button type={type} disabled={disabled} className={'btn ' + variant + (small?' sm':'')} onClick={onClick}>{children}</button>;
/** A headline number with its label and a note, for the stats row. */
export const Stat = ({ label, value, note }) => <div className="stat"><span className="eyebrow">{label}</span><span className="stat-value">{value}</span><span className="stat-note">{note}</span></div>;
/** A card with a title, an optional action in its header and an optional footer. */
export const Panel = ({ title, action, footer, children }) => <section className="card"><div className="card-head"><h2>{title}</h2>{action}</div>{children}{footer && <div className="card-foot">{footer}</div>}</section>;
/** options: plain strings, or [value, text] pairs. */
export const Select = ({ label, value, onChange, options, disabled=false, required=false }) => <select className="sel" aria-label={label} value={value} disabled={disabled} required={required} onChange={(e)=>onChange(e.target.value)}>{options.map(x=>{const [v,t]=Array.isArray(x)?x:[x,x];return <option key={v} value={v}>{t}</option>;})}</select>;
/** A labelled form field with an optional hint and error message. */
export const Field = ({ label, children, hint, error }) => <label className="field"><span>{label}</span>{hint && <small>{hint}</small>}{children}{error && <span className="error">{error}</span>}</label>;
/** The page header: the API route as an eyebrow, the title and the page's actions. */
export const Header = ({ page, title, actions, route }) => <header className="topbar"><div className="topbar-title"><span className="eyebrow">{route||routes[page]}</span><h1>{title||labels[page]}</h1></div><div className="topbar-actions">{actions}</div></header>;
/** Initials of a name ("Last, First" or "First Last"). */
export const Avatar = ({ name }) => <span className="person-avatar">{(name.includes(', ') ? name.split(', ').reverse() : name.split(' ')).slice(0,2).map(x=>x[0]).join('').toUpperCase()}</span>;
/** A modal form: closes on Escape or a click outside; `busy` disables the submit button while saving. */
export function Dialog({ title, children, close, submit, action='Save', busy=false }) {
  useEffect(()=>{ const onKey=(e)=>{if(e.key==='Escape')close();}; document.addEventListener('keydown',onKey); return ()=>document.removeEventListener('keydown',onKey); },[close]);
  return <div className="modal-backdrop" onMouseDown={(e)=>{if(e.target===e.currentTarget)close();}}><form className="modal" onSubmit={submit} aria-label={title}><div className="card-head"><h2>{title}</h2><Button onClick={close} small>✕</Button></div><div className="card-body stack">{children}</div><div className="card-foot"><Button onClick={close}>Cancel</Button><Button type="submit" variant="primary" disabled={busy}>{busy?'Saving…':action}</Button></div></form></div>;
}
/** Shows an API error (ProblemDetails message) with an optional retry. */
export const ApiNotice = ({ error, retry }) => error ? <div className="note bad" role="alert"><span>{error.message}</span>{retry&&<Button small onClick={retry}>Retry</Button>}</div> : null;
/** Placeholder row while a table's first page loads, or when it is empty. */
export const Empty = ({ loading, children }) => <div className="empty-state">{loading?'Loading…':children}</div>;
