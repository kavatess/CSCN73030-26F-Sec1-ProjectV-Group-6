/**
 * @file screens.js
 * @brief Screen titles, the API route shown above each title, and the CSV export helper.
 */

export const labels = { overview:'Workforce overview', directory:'Staff directory', roster:'Duty roster', requisitions:'Requisitions', credentials:'Credentials & licence expiry', profile:'Employee profile', onboard:'Onboard employee' };
export const routes = { overview:'CITY MANAGEMENT ENTERPRISE SYSTEM', directory:'GET /STAFF?DEPARTMENT=&SERVICE=&ROLE=&STATUS=&PAGE=', roster:'GET /ROSTERS?SERVICE=PARAMEDIC&FROM=2026-03-09&TO=2026-03-15', requisitions:'POST /REQUISITIONS · GET /REQUISITIONS?STATUS=', credentials:'GET /CREDENTIALS?STATE=&EXPIRESBEFORE=', profile:'← STAFF DIRECTORY / GET /STAFF/STF-0002741', onboard:'← STAFF DIRECTORY / POST /STAFF' };
export function csvDownload(name, headings, rows) {
  const quote = (v) => '"' + String(v??'').replaceAll('"','""') + '"';
  const text = [headings,...rows].map(row=>row.map(quote).join(',')).join('\r\n');
  const url = URL.createObjectURL(new Blob([text],{type:'text/csv;charset=utf-8'}));
  const a = document.createElement('a'); a.href=url; a.download=name; a.click();
  setTimeout(()=>URL.revokeObjectURL(url),1000);
}
