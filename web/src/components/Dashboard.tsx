import { useState } from 'react'
import { formatLimit } from '../report/format'
import type { Report, RuleName } from '../report/schema'
import { FindingDetail } from './FindingDetail'
import { FindingList } from './FindingList'
import { RuleFilter } from './RuleFilter'

type Props = {
  report: Report
}

export function Dashboard({ report }: Props) {
  const [ruleFilter, setRuleFilter] = useState<RuleName | null>(null)
  const [selectedIndex, setSelectedIndex] = useState(0)

  const visible = report.findings
    .map((finding, index) => ({ finding, index }))
    .filter(({ finding }) => ruleFilter === null || finding.rule === ruleFilter)
  const selected = visible.find((item) => item.index === selectedIndex) ?? visible[0]

  const { summary, thresholds } = report
  const findingsLabel = `${summary.findings} finding${summary.findings === 1 ? '' : 's'}`

  function handleFilterChange(rule: RuleName | null) {
    setRuleFilter(rule)
    setSelectedIndex(-1) // start from the first visible finding, not one picked under another filter
  }

  return (
    <>
      <section className="overview" aria-labelledby="overview-heading">
        <p className="overview__checked">
          Checked {summary.invoices} invoices and {summary.orders} orders
        </p>
        <h2 id="overview-heading" className="overview__count">
          {findingsLabel}
        </h2>
        <RuleFilter findings={report.findings} active={ruleFilter} onChange={handleFilterChange} />
      </section>

      <p className="workspace__count" role="status">
        Showing {visible.length} of {report.findings.length} findings
      </p>
      <div className="workspace">
        <FindingList items={visible} selectedIndex={selected?.index} onSelect={setSelectedIndex} />
        {selected ? (
          <FindingDetail finding={selected.finding} />
        ) : (
          <p className="workspace__empty">No findings to show.</p>
        )}
      </div>

      <p className="thresholds">
        Thresholds: duplicate window {thresholds.duplicateWindowDays} days · split window {thresholds.splitWindowDays}{' '}
        days · approval limit {formatLimit(thresholds.approvalLimit)} · working hours {thresholds.workdayStart}–
        {thresholds.workdayEnd}
      </p>
    </>
  )
}
