import { z } from 'zod'

/** The rule names the CLI writes; the page refuses anything else rather than guess how to explain it. */
export const ruleNames = ['Duplicate invoice', 'Split order', 'Self-approval', 'Off-hours approval'] as const

const invoiceDocument = z.object({
  kind: z.literal('invoice'),
  id: z.string(),
  supplierId: z.string(),
  amount: z.number(),
  invoiceNumber: z.string(),
  date: z.string(),
})

const orderDocument = z.object({
  kind: z.literal('order'),
  id: z.string(),
  supplierId: z.string(),
  amount: z.number(),
  requesterId: z.string(),
  approverId: z.string(),
  approvedAt: z.string(),
})

const count = z.number().int().nonnegative()

export const reportSchema = z.object({
  summary: z.object({ invoices: count, orders: count, findings: count }),
  thresholds: z.object({
    duplicateWindowDays: count,
    splitWindowDays: count,
    approvalLimit: z.number().positive(),
    workdayStart: z.string(),
    workdayEnd: z.string(),
  }),
  findings: z.array(
    z.object({
      rule: z.enum(ruleNames),
      reason: z.string(),
      documents: z.array(z.discriminatedUnion('kind', [invoiceDocument, orderDocument])).min(1),
    }),
  ),
})

export type Report = z.infer<typeof reportSchema>
export type Finding = Report['findings'][number]
export type AuditDocument = Finding['documents'][number]
export type RuleName = (typeof ruleNames)[number]
