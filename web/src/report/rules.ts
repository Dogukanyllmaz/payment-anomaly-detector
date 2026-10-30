import type { RuleName } from './schema'

interface RuleInfo {
  /** CSS modifier that gives the rule its colour everywhere on the page. */
  tone: 'duplicate' | 'split' | 'self' | 'offhours'
  summary: string
  why: string
}

export const ruleInfo: Record<RuleName, RuleInfo> = {
  'Duplicate invoice': {
    tone: 'duplicate',
    summary: 'Same supplier, same amount, a few days apart',
    why: 'The most common cause of overpayment. Invoice numbers often differ, so matching on the number alone misses it.',
  },
  'Split order': {
    tone: 'split',
    summary: 'Orders under the limit that add up past it',
    why: 'Cutting one purchase into pieces keeps each under the approval limit and skips the higher approval level.',
  },
  'Self-approval': {
    tone: 'self',
    summary: 'Requester approved their own order',
    why: 'No segregation of duties: the same person asked for the spend and authorised it.',
  },
  'Off-hours approval': {
    tone: 'offhours',
    summary: 'Approved at the weekend or at night',
    why: 'Approvals given when nobody is likely to review them, and a common sign of shared or delegated credentials.',
  },
}
