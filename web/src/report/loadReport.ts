import { reportSchema, type Report } from './schema'

/** Reads the JSON written by `paymentanomalies --json` from `${baseUrl}data/report.json`. */
export async function loadReport(baseUrl: string, fetchFn: typeof fetch = fetch): Promise<Report> {
  const root = baseUrl.endsWith('/') ? baseUrl : `${baseUrl}/`
  const response = await fetchFn(`${root}data/report.json`)
  if (!response.ok) throw new Error(`Could not load the audit report (HTTP ${response.status})`)

  const formatError = new Error('The audit report does not have the expected format.')
  let body: unknown
  try {
    body = await response.json()
  } catch {
    // static hosts answer some missing files with an HTML page; that is a format problem, not a parser detail
    throw formatError
  }

  const parsed = reportSchema.safeParse(body)
  if (!parsed.success) throw formatError
  return parsed.data
}
