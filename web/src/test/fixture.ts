import type { Report } from '../report/schema'

/** A trimmed copy of what `paymentanomalies --json` writes for the sample data. */
export const report: Report = {
  summary: { invoices: 12, orders: 12, findings: 3 },
  thresholds: {
    duplicateWindowDays: 7,
    splitWindowDays: 3,
    approvalLimit: 100000,
    workdayStart: '07:00',
    workdayEnd: '20:00',
  },
  findings: [
    {
      rule: 'Duplicate invoice',
      reason: 'SUP-104 billed 18,450.00 twice, 3 day(s) apart',
      documents: [
        { kind: 'invoice', id: 'INV-004', supplierId: 'SUP-104', amount: 18450, invoiceNumber: 'F-55012', date: '2026-03-10' },
        { kind: 'invoice', id: 'INV-006', supplierId: 'SUP-104', amount: 18450, invoiceNumber: 'F-55031', date: '2026-03-13' },
      ],
    },
    {
      rule: 'Self-approval',
      reason: 'a.celik approved their own order',
      documents: [
        { kind: 'order', id: 'PO-005', supplierId: 'SUP-203', amount: 8750, requesterId: 'a.celik', approverId: 'a.celik', approvedAt: '2026-03-05T14:20' },
      ],
    },
    {
      rule: 'Off-hours approval',
      reason: 'Approved by m.demir on Mon 2026-03-09 22:15',
      documents: [
        { kind: 'order', id: 'PO-008', supplierId: 'SUP-201', amount: 15300, requesterId: 'a.celik', approverId: 'm.demir', approvedAt: '2026-03-09T22:15' },
      ],
    },
  ],
}
