import { formatAmount, formatTimestamp } from '../report/format'
import { ruleInfo } from '../report/rules'
import type { AuditDocument, Finding } from '../report/schema'

type Props = {
  finding: Finding
}

export function FindingDetail({ finding }: Props) {
  const info = ruleInfo[finding.rule]
  const total = finding.documents.reduce((sum, document) => sum + document.amount, 0)
  const isInvoice = finding.documents[0].kind === 'invoice'

  return (
    <section className={`finding-detail tone-${info.tone}`} aria-label="Finding detail">
      <p className="finding-detail__rule">{finding.rule}</p>
      <h3 className="finding-detail__reason">{finding.reason}</h3>
      <p className="finding-detail__why">
        <strong>Why it matters.</strong> {info.why}
      </p>

      <table className="ledger" aria-label="Documents behind this finding">
        <thead>
          {isInvoice ? (
            <tr>
              <th scope="col">Invoice</th>
              <th scope="col">Number</th>
              <th scope="col">Supplier</th>
              <th scope="col">Date</th>
              <th scope="col" className="ledger__amount">Amount</th>
            </tr>
          ) : (
            <tr>
              <th scope="col">Order</th>
              <th scope="col">Requester</th>
              <th scope="col">Approver</th>
              <th scope="col">Supplier</th>
              <th scope="col">Approved at</th>
              <th scope="col" className="ledger__amount">Amount</th>
            </tr>
          )}
        </thead>
        <tbody>
          {finding.documents.map((document) => (
            <DocumentRow key={document.id} document={document} />
          ))}
        </tbody>
        {finding.documents.length > 1 && (
          <tfoot>
            <tr>
              <th scope="row" colSpan={isInvoice ? 4 : 5}>
                Total
              </th>
              <td className="ledger__amount">{formatAmount(total)}</td>
            </tr>
          </tfoot>
        )}
      </table>
    </section>
  )
}

function DocumentRow({ document }: { document: AuditDocument }) {
  if (document.kind === 'invoice') {
    return (
      <tr>
        <td>{document.id}</td>
        <td>{document.invoiceNumber}</td>
        <td>{document.supplierId}</td>
        <td>{document.date}</td>
        <td className="ledger__amount">{formatAmount(document.amount)}</td>
      </tr>
    )
  }

  const isSamePerson = document.requesterId.toLowerCase() === document.approverId.toLowerCase()
  return (
    <tr>
      <td>{document.id}</td>
      <td className={isSamePerson ? 'ledger__flag' : undefined}>{document.requesterId}</td>
      <td className={isSamePerson ? 'ledger__flag' : undefined}>{document.approverId}</td>
      <td>{document.supplierId}</td>
      <td>{formatTimestamp(document.approvedAt)}</td>
      <td className="ledger__amount">{formatAmount(document.amount)}</td>
    </tr>
  )
}
