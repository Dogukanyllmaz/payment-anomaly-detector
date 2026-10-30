import { report } from '../test/fixture'
import { loadReport } from './loadReport'

function respond(status: number, body: unknown): typeof fetch {
  return vi.fn(async () => new Response(JSON.stringify(body), { status })) as unknown as typeof fetch
}

describe('loadReport', () => {
  it('reads data/report.json under the base URL and validates it', async () => {
    const fetchFn = respond(200, report)

    await expect(loadReport('./', fetchFn)).resolves.toEqual(report)
    expect(fetchFn).toHaveBeenCalledWith('./data/report.json')
  })

  it('reports an HTTP error in plain words', async () => {
    await expect(loadReport('./', respond(404, {}))).rejects.toThrow('Could not load the audit report (HTTP 404)')
  })

  it('explains a page that is not JSON at all instead of showing a parser error', async () => {
    const html = vi.fn(async () => new Response('<!doctype html>', { status: 200 })) as unknown as typeof fetch

    await expect(loadReport('./', html)).rejects.toThrow('The audit report does not have the expected format.')
  })

  it('refuses data that does not match the expected shape', async () => {
    await expect(loadReport('./', respond(200, { findings: 'nope' }))).rejects.toThrow()
  })
})
