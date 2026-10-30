import { report } from '../test/fixture'
import { reportSchema } from './schema'

describe('reportSchema', () => {
  it('accepts the report the CLI writes', () => {
    expect(reportSchema.parse(report)).toEqual(report)
  })

  it('rejects a rule the page does not know how to explain', () => {
    const changed = { ...report, findings: [{ ...report.findings[0], rule: 'Unknown rule' }] }

    expect(reportSchema.safeParse(changed).success).toBe(false)
  })

  it('rejects a document of an unknown kind', () => {
    const document = { ...report.findings[0].documents[0], kind: 'payment' }
    const changed = { ...report, findings: [{ ...report.findings[0], documents: [document] }] }

    expect(reportSchema.safeParse(changed).success).toBe(false)
  })

  it('rejects a finding without documents', () => {
    const changed = { ...report, findings: [{ ...report.findings[0], documents: [] }] }

    expect(reportSchema.safeParse(changed).success).toBe(false)
  })
})
