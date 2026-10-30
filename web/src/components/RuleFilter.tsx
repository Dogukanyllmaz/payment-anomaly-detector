import { ruleInfo } from '../report/rules'
import { ruleNames, type Finding, type RuleName } from '../report/schema'

type Props = {
  findings: Finding[]
  active: RuleName | null
  onChange: (rule: RuleName | null) => void
}

export function RuleFilter({ findings, active, onChange }: Props) {
  const countOf = (rule: RuleName) => findings.filter((finding) => finding.rule === rule).length

  return (
    <div className="rule-filter" role="group" aria-label="Filter by rule">
      <button
        type="button"
        className="rule-tile rule-tile--all"
        aria-pressed={active === null}
        onClick={() => onChange(null)}
      >
        <span className="rule-tile__count">{findings.length}</span>
        <span className="rule-tile__name">All rules</span>
      </button>
      {ruleNames.map((rule) => (
        <button
          key={rule}
          type="button"
          className={`rule-tile tone-${ruleInfo[rule].tone}`}
          aria-pressed={active === rule}
          onClick={() => onChange(rule)}
        >
          <span className="rule-tile__count">{countOf(rule)}</span>
          <span className="rule-tile__name">{rule}</span>
          <span className="rule-tile__summary">{ruleInfo[rule].summary}</span>
        </button>
      ))}
    </div>
  )
}
