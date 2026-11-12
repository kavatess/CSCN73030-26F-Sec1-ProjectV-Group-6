// Presentation data mirrors the supplied Staff & HR design. Replace with API responses later.
export const departments = [
  { name: 'Emergency Services', code: 'EMS', active: 412, duty: 128, vacancies: 7, payroll: '$3.41M' },
  { name: 'Security (Police)', code: 'SEC', active: 338, duty: 96, vacancies: 4, payroll: '$2.88M' },
  { name: 'Public Transport', code: 'PTR', active: 246, duty: 58, vacancies: 11, payroll: '$1.62M' },
  { name: 'Maintenance', code: 'MNT', active: 201, duty: 24, vacancies: 6, payroll: '$1.33M' },
  { name: 'Budget', code: 'BUD', active: 42, duty: null, vacancies: 1, payroll: '$0.39M' },
  { name: 'Residence', code: 'RES', active: 31, duty: 6, vacancies: 0, payroll: '$0.22M' },
  { name: 'Staff / HR', code: 'HRS', active: 14, duty: null, vacancies: 0, payroll: '$0.12M' },
];

export const initialStaff = [
  { id: 'STF-0004217', name: 'Raghunathan, Priya', department: 'Emergency Services', service: 'Paramedic Service', role: 'Primary Care Paramedic', status: 'Active', credential: 'Valid', duty: '06:00–18:00' },
  { id: 'STF-0003118', name: 'Oyelaran, Nadia', department: 'Security (Police)', service: 'Patrol Division', role: 'Constable', status: 'Suspended', credential: 'Flagged', duty: 'Not eligible' },
  { id: 'STF-0002741', name: 'Delacroix, Marcus', department: 'Public Transport', service: 'Bus Operations', role: 'Transit Operator', status: 'Active', credential: 'Expires in 12d', duty: '05:30–13:30' },
  { id: 'STF-0001902', name: 'Lindqvist, Hanne', department: 'Emergency Services', service: 'Fire Suppression', role: 'Fire Captain', status: 'Active', credential: 'Valid', duty: 'Rest day' },
  { id: 'STF-0003996', name: 'Mwangi, Tobias', department: 'Maintenance', service: 'Electrical Works', role: 'Licensed Electrician', status: 'Active', credential: 'Valid', duty: '07:00–15:00' },
  { id: 'STF-0004102', name: 'Marchetti, Sofia', department: 'Public Transport', service: 'Bus Operations', role: 'Transit Operator', status: 'Probation', credential: 'Expires in 6d', duty: '13:30–21:30' },
  { id: 'STF-0002155', name: 'Yildirim, Emre', department: 'Security (Police)', service: 'Patrol Division', role: 'Sergeant', status: 'Active', credential: 'Valid', duty: '22:00–06:00' },
  { id: 'STF-0003470', name: 'Abellard, Grace', department: 'Emergency Services', service: 'Paramedic Service', role: 'Advanced Care Paramedic', status: 'On leave', credential: 'Valid', duty: 'Not eligible' },
  { id: 'STF-0004188', name: 'Sokolov, Dmitri', department: 'Maintenance', service: 'Water & Lines', role: 'Pipefitter', status: 'Active', credential: 'Expired', duty: 'Not eligible' },
  { id: 'STF-0001336', name: 'Tanabe, Aiko', department: 'Staff / HR', service: 'HR Operations', role: 'HR Administrator', status: 'Active', credential: 'Not required', duty: '09:00–17:00' },
];

export const initialRequisitions = [
  { id: 'REQ-2026-0191', type: 'Hire', department: 'Emergency Services', role: 'Primary Care Paramedic', posts: 3, budget: 'Pending', status: 'Awaiting Budget', raised: '10 Mar', reason: 'Night cover shortfall, Paramedic Service', key: 'ems-8f3c91a4-2026-03-10' },
  { id: 'REQ-2026-0188', type: 'Hire', department: 'Public Transport', role: 'Transit Operator', posts: 2, budget: 'Approved', status: '1 of 2 filled', raised: '02 Mar', reason: 'Bus Operations expansion', key: 'ptr-2cf461b0-2026-03-02' },
  { id: 'REQ-2026-0185', type: 'Termination', department: 'Security (Police)', role: 'Constable', posts: 1, budget: 'Notified', status: 'Completed', raised: '27 Feb', reason: 'Department request', key: 'sec-7d9a-2026-02-27' },
  { id: 'REQ-2026-0184', type: 'Hire', department: 'Maintenance', role: 'Licensed Electrician', posts: 1, budget: 'Rejected', status: 'Closed — no funding', raised: '25 Feb', reason: 'Electrical Works headcount', key: 'mnt-2f10-2026-02-25' },
  { id: 'REQ-2026-0179', type: 'Termination', department: 'Public Transport', role: 'Transit Operator', posts: 1, budget: 'Not required', status: 'HR review', raised: '21 Feb', reason: 'Department request', key: 'ptr-3c71-2026-02-21' },
  { id: 'REQ-2026-0176', type: 'Hire', department: 'Security (Police)', role: 'Sergeant', posts: 1, budget: 'Approved', status: 'Completed', raised: '18 Feb', reason: 'Patrol staffing', key: 'sec-3d17-2026-02-18' },
];

export const initialCredentials = [
  { staff: 'Sokolov, Dmitri', unit: 'MNT · Pipefitter', department: 'Maintenance', type: 'Backflow prevention', number: 'BP-2210-55', expires: '2026-02-11', remaining: '33 days overdue', state: 'Expired' },
  { staff: 'Sørensen, Elias', unit: 'EMS · Paramedic', department: 'Emergency Services', type: 'ACP certification', number: 'ACP-1180-09', expires: '2026-02-02', remaining: '42 days overdue', state: 'Expired' },
  { staff: 'Oyelaran, Nadia', unit: 'SEC · Constable', department: 'Security (Police)', type: 'Police clearance', number: 'PC-5501-77', expires: '2027-01-18', remaining: 'Held by Security', state: 'Suspended' },
  { staff: 'Marchetti, Sofia', unit: 'PTR · Transit Operator', department: 'Public Transport', type: 'Class D driver licence', number: 'DL-5120-3318', expires: '2026-03-22', remaining: '6 d', state: 'Critical' },
  { staff: 'Petrov, Anya', unit: 'SEC · Constable', department: 'Security (Police)', type: 'Firearms qualification', number: 'FQ-8821-04', expires: '2026-03-24', remaining: '8 d', state: 'Critical' },
  { staff: 'Delacroix, Marcus', unit: 'PTR · Transit Operator', department: 'Public Transport', type: 'Class D driver licence', number: 'DL-4471-9920', expires: '2026-03-29', remaining: '12 d', state: 'Expiring' },
  { staff: 'Nakamura, Ren', unit: 'PTR · Transit Operator', department: 'Public Transport', type: 'Passenger endorsement F', number: 'PE-F-30192', expires: '2026-03-30', remaining: '13 d', state: 'Expiring' },
  { staff: 'Chen, Wei-Lin', unit: 'EMS · Paramedic', department: 'Emergency Services', type: 'PCP certification', number: 'PCP-2240-18', expires: '2026-04-06', remaining: '21 d', state: 'Expiring' },
  { staff: 'Mwangi, Tobias', unit: 'MNT · Electrician', department: 'Maintenance', type: 'Master electrician licence', number: 'ME-7788-21', expires: '2026-09-14', remaining: '182 d', state: 'Valid' },
  { staff: 'Yildirim, Emre', unit: 'SEC · Sergeant', department: 'Security (Police)', type: 'Firearms qualification', number: 'FQ-9013-11', expires: '2026-11-02', remaining: '231 d', state: 'Valid' },
];

export const rosterStaff = [
  { name: 'RAGHUNATHAN, PRIYA', initials: 'PR', detail: 'PCP · STF-0004217', shifts: ['D','D','','D','D','D',''], hours: 40 },
  { name: 'NOVAK, PETRA', initials: 'PN', detail: 'PCP · STF-0003205', shifts: ['','D','D','D','D','','D'], hours: 40 },
  { name: 'CHEN, WEI-LIN', initials: 'WC', detail: 'PCP · CERT EXPIRES IN 21D', shifts: ['D','','D','','','D','D'], hours: 32 },
  { name: 'FERREIRA, LUÍSA', initials: 'LF', detail: 'ACP · STF-0002618', shifts: ['E','','E','E','','','E'], hours: 32 },
  { name: 'IVANOVIĆ, MILA', initials: 'MI', detail: 'PCP · PART-TIME', shifts: ['','E','','','E','E',''], hours: 24 },
  { name: 'OKONJO, DANIEL', initials: 'DO', detail: 'ACP · STF-0001774', shifts: ['N','N','N','','','N','N'], hours: 40 },
  { name: 'HADDAD, KARIM', initials: 'KH', detail: 'ACP · STF-0002903', shifts: ['N','','N','N','N','','N'], hours: 40 },
  { name: 'BRENNAN, ÓRLA', initials: 'ÓB', detail: 'ACP · PART-TIME', shifts: ['','N','','N','N','',''], hours: 24 },
  { name: 'ABELLARD, GRACE', initials: 'GA', detail: 'ACP · ON PARENTAL LEAVE', shifts: ['','','','','','',''], hours: 0, blocked: true },
];
