import { useEffect, useState } from 'react'
import { Dashboard } from './components/Dashboard'
import { loadReport } from './report/loadReport'
import type { Report } from './report/schema'

const repositoryUrl = 'https://github.com/Dogukanyllmaz/payment-anomaly-detector'

type Props = {
  load?: () => Promise<Report>
}

type State =
  | { status: 'loading' }
  | { status: 'error'; message: string }
  | { status: 'ready'; report: Report }

const loadPublishedReport = () => loadReport(import.meta.env.BASE_URL)

export default function App({ load = loadPublishedReport }: Props) {
  const [state, setState] = useState<State>({ status: 'loading' })

  useEffect(() => {
    let isActive = true
    load().then(
      (report) => {
        if (isActive) setState({ status: 'ready', report })
      },
      (error: unknown) => {
        const message = error instanceof Error ? error.message : 'Unexpected error while loading the report.'
        if (isActive) setState({ status: 'error', message })
      },
    )
    return () => {
      isActive = false
    }
  }, [load])

  return (
    <div className="page">
      <header className="masthead">
        <p className="masthead__eyebrow">Continuous auditing · procure-to-pay · synthetic data</p>
        <h1>Payment Anomaly Detector</h1>
        <p className="masthead__lede">
          Every purchase order and invoice in the export is tested for duplicate payments, purchases split under the
          approval limit, self-approval and off-hours approval. Each finding comes with the documents behind it.
        </p>
        <a className="masthead__link" href={repositoryUrl} target="_blank" rel="noopener noreferrer">
          Source on GitHub
        </a>
      </header>

      <main>
        {state.status === 'loading' && (
          <p className="status" role="status">
            Loading the audit report…
          </p>
        )}
        {state.status === 'error' && (
          <p className="status status--error" role="alert">
            {state.message}
          </p>
        )}
        {state.status === 'ready' && <Dashboard report={state.report} />}
      </main>

      <footer className="colophon">
        Findings computed by the <code>paymentanomalies</code> CLI (.NET 10) on the sample data and published by CI.
      </footer>
    </div>
  )
}
