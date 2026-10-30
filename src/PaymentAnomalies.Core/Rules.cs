using static System.FormattableString;

namespace PaymentAnomalies;

public interface IRule
{
    IEnumerable<Finding> Evaluate(AuditData data);
}

public static class Audit
{
    /// <summary>Runs every rule over the data; findings are grouped by rule, in rule order.</summary>
    public static IReadOnlyList<Finding> Run(AuditData data, RuleOptions options)
    {
        IRule[] rules =
        [
            new DuplicateInvoiceRule(options),
            new SplitOrderRule(options),
            new SelfApprovalRule(),
            new OffHoursApprovalRule(options)
        ];
        return rules.SelectMany(rule => rule.Evaluate(data)).ToList();
    }
}

/// <summary>Same supplier billing the same amount twice within a few days: the classic double payment.</summary>
public sealed class DuplicateInvoiceRule(RuleOptions options) : IRule
{
    public IEnumerable<Finding> Evaluate(AuditData data) =>
        from g in data.Invoices.GroupBy(i => (Supplier: i.SupplierId.ToUpperInvariant(), i.Amount))
        let sorted = g.OrderBy(i => i.Date).ToList()
        from pair in sorted.Zip(sorted.Skip(1))
        let days = pair.Second.Date.DayNumber - pair.First.Date.DayNumber
        where days <= options.DuplicateWindowDays
        select new Finding("Duplicate invoice", [pair.First.Id, pair.Second.Id],
            Invariant($"{pair.First.SupplierId} billed {pair.First.Amount:N2} twice, {days} day(s) apart"));
}

/// <summary>
/// Several orders from the same requester to the same supplier, each under the approval limit
/// but together over it: a purchase split to avoid the higher approval level.
/// </summary>
public sealed class SplitOrderRule(RuleOptions options) : IRule
{
    public IEnumerable<Finding> Evaluate(AuditData data)
    {
        var window = TimeSpan.FromDays(options.SplitWindowDays);
        var groups = data.Orders
            .Where(o => o.Amount < options.ApprovalLimit) // larger orders already went to the higher level
            .GroupBy(o => (Requester: o.RequesterId.ToUpperInvariant(), Supplier: o.SupplierId.ToUpperInvariant()));

        foreach (var group in groups)
        {
            var sorted = group.OrderBy(o => o.ApprovedAt).ToList();
            var start = 0;
            while (start < sorted.Count)
            {
                var cluster = sorted.Skip(start)
                    .TakeWhile(o => o.ApprovedAt - sorted[start].ApprovedAt <= window)
                    .ToList();
                var total = cluster.Sum(o => o.Amount);

                if (cluster.Count < 2 || total < options.ApprovalLimit)
                {
                    start++;
                    continue;
                }

                var days = (cluster[^1].ApprovedAt - cluster[0].ApprovedAt).Days;
                yield return new Finding("Split order", cluster.Select(o => o.Id).ToList(),
                    Invariant($"{cluster[0].RequesterId} placed {cluster.Count} orders with {cluster[0].SupplierId} within {days} day(s), total {total:N0}, each below the {options.ApprovalLimit:N0} limit"));
                start += cluster.Count;
            }
        }
    }
}

/// <summary>Requester approved their own order: no segregation of duties.</summary>
public sealed class SelfApprovalRule : IRule
{
    public IEnumerable<Finding> Evaluate(AuditData data) =>
        from o in data.Orders
        where string.Equals(o.RequesterId, o.ApproverId, StringComparison.OrdinalIgnoreCase)
        select new Finding("Self-approval", [o.Id], $"{o.RequesterId} approved their own order");
}

/// <summary>Approval given at the weekend or outside working hours, when review is least likely.</summary>
public sealed class OffHoursApprovalRule(RuleOptions options) : IRule
{
    public IEnumerable<Finding> Evaluate(AuditData data) =>
        from o in data.Orders
        let time = TimeOnly.FromDateTime(o.ApprovedAt)
        where o.ApprovedAt.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
              || time < options.WorkdayStart
              || time >= options.WorkdayEnd
        select new Finding("Off-hours approval", [o.Id],
            Invariant($"Approved by {o.ApproverId} on {o.ApprovedAt:ddd yyyy-MM-dd HH:mm}"));
}
