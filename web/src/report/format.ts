const amountFormat = new Intl.NumberFormat('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const limitFormat = new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 })

export function formatAmount(amount: number): string {
  return amountFormat.format(amount)
}

export function formatLimit(amount: number): string {
  return limitFormat.format(amount)
}

/** `2026-03-09T22:15` → `2026-03-09 22:15` */
export function formatTimestamp(value: string): string {
  return value.replace('T', ' ')
}
