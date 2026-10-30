import { ruleInfo } from '../report/rules'
import type { Finding } from '../report/schema'

type Props = {
  items: { finding: Finding; index: number }[]
  selectedIndex: number | undefined
  onSelect: (index: number) => void
}

export function FindingList({ items, selectedIndex, onSelect }: Props) {
  return (
    <ul className="finding-list" aria-label="Findings">
      {items.map(({ finding, index }) => (
        // the report is static and `index` is the finding's position in it, so it is a stable identity
        <li key={index}>
          <button
            type="button"
            className={`finding-row tone-${ruleInfo[finding.rule].tone}`}
            aria-current={index === selectedIndex ? 'true' : undefined}
            onClick={() => onSelect(index)}
          >
            <span className="finding-row__rule">{finding.rule}</span>
            <span className="finding-row__documents">{finding.documents.map((d) => d.id).join(' · ')}</span>
            <span className="finding-row__reason">{finding.reason}</span>
          </button>
        </li>
      ))}
    </ul>
  )
}
