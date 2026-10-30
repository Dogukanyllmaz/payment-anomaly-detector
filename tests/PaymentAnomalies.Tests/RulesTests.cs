using System.Globalization;

namespace PaymentAnomalies.Tests;

public class RulesTests
{
    private static readonly RuleOptions Options = new();

    private static Invoice Inv(string id, string supplier, string date, decimal amount) =>
        new(id, supplier, $"N-{id}", DateOnly.Parse(date, CultureInfo.InvariantCulture), amount);

    private static PurchaseOrder Po(string id, decimal amount, string approvedAt,
        string requester = "u.kaya", string approver = "m.demir", string supplier = "SUP-1") =>
        new(id, requester, approver, supplier, amount, DateTime.Parse(approvedAt, CultureInfo.InvariantCulture));

    private static AuditData Invoices(params Invoice[] invoices) => new(invoices, []);
    private static AuditData Orders(params PurchaseOrder[] orders) => new([], orders);

    // Duplicate invoice

    [Fact]
    public void Duplicate_flags_same_supplier_and_amount_on_the_last_day_of_the_window()
    {
        var data = Invoices(Inv("I1", "SUP-1", "2026-05-01", 1500m), Inv("I2", "SUP-1", "2026-05-08", 1500m));

        var finding = Assert.Single(new DuplicateInvoiceRule(Options).Evaluate(data));

        Assert.Equal(["I1", "I2"], finding.DocumentIds);
    }

    [Fact]
    public void Duplicate_ignores_same_amount_outside_the_window()
    {
        var data = Invoices(Inv("I1", "SUP-1", "2026-05-01", 1500m), Inv("I2", "SUP-1", "2026-05-09", 1500m));

        Assert.Empty(new DuplicateInvoiceRule(Options).Evaluate(data));
    }

    [Fact]
    public void Duplicate_matches_supplier_codes_regardless_of_case()
    {
        var data = Invoices(Inv("I1", "SUP-1", "2026-05-01", 1500m), Inv("I2", "sup-1", "2026-05-02", 1500m));

        Assert.Single(new DuplicateInvoiceRule(Options).Evaluate(data));
    }

    [Fact]
    public void Duplicate_ignores_same_amount_from_different_suppliers()
    {
        var data = Invoices(Inv("I1", "SUP-1", "2026-05-01", 1500m), Inv("I2", "SUP-2", "2026-05-02", 1500m));

        Assert.Empty(new DuplicateInvoiceRule(Options).Evaluate(data));
    }

    [Fact]
    public void Duplicate_reports_a_chain_of_three_as_two_consecutive_pairs()
    {
        var data = Invoices(
            Inv("I1", "SUP-1", "2026-05-01", 1500m),
            Inv("I2", "SUP-1", "2026-05-03", 1500m),
            Inv("I3", "SUP-1", "2026-05-05", 1500m));

        var findings = new DuplicateInvoiceRule(Options).Evaluate(data).ToList();

        Assert.Equal([["I1", "I2"], ["I2", "I3"]], findings.Select(f => f.DocumentIds));
    }

    // Split order

    [Fact]
    public void Split_flags_a_total_exactly_at_the_limit()
    {
        var data = Orders(Po("P1", 50_000m, "2026-05-04T10:00"), Po("P2", 50_000m, "2026-05-04T11:00"));

        Assert.Single(new SplitOrderRule(Options).Evaluate(data));
    }

    [Fact]
    public void Split_groups_orders_exactly_the_window_apart()
    {
        var data = Orders(Po("P1", 60_000m, "2026-05-04T10:00"), Po("P2", 60_000m, "2026-05-07T10:00"));

        Assert.Single(new SplitOrderRule(Options).Evaluate(data));
    }

    [Fact]
    public void Split_flags_orders_below_the_limit_that_together_reach_it()
    {
        var data = Orders(
            Po("P1", 40_000m, "2026-05-04T10:00"),
            Po("P2", 35_000m, "2026-05-05T10:00"),
            Po("P3", 30_000m, "2026-05-06T10:00"));

        var finding = Assert.Single(new SplitOrderRule(Options).Evaluate(data));

        Assert.Equal(["P1", "P2", "P3"], finding.DocumentIds);
        Assert.Contains("105,000", finding.Reason);
    }

    [Fact]
    public void Split_ignores_orders_whose_total_stays_below_the_limit()
    {
        var data = Orders(Po("P1", 40_000m, "2026-05-04T10:00"), Po("P2", 59_999m, "2026-05-05T10:00"));

        Assert.Empty(new SplitOrderRule(Options).Evaluate(data));
    }

    [Fact]
    public void Split_ignores_orders_spread_beyond_the_window()
    {
        var data = Orders(Po("P1", 60_000m, "2026-05-04T10:00"), Po("P2", 60_000m, "2026-05-08T10:00"));

        Assert.Empty(new SplitOrderRule(Options).Evaluate(data));
    }

    [Fact]
    public void Split_ignores_orders_at_the_limit_because_they_already_needed_higher_approval()
    {
        var data = Orders(Po("P1", 100_000m, "2026-05-04T10:00"), Po("P2", 10_000m, "2026-05-04T11:00"));

        Assert.Empty(new SplitOrderRule(Options).Evaluate(data));
    }

    [Fact]
    public void Split_does_not_group_orders_of_different_requesters()
    {
        var data = Orders(
            Po("P1", 60_000m, "2026-05-04T10:00", requester: "u.kaya"),
            Po("P2", 60_000m, "2026-05-04T11:00", requester: "a.celik"));

        Assert.Empty(new SplitOrderRule(Options).Evaluate(data));
    }

    [Fact]
    public void Split_treats_requester_ids_differing_only_in_case_as_one_person()
    {
        var data = Orders(
            Po("P1", 60_000m, "2026-05-04T10:00", requester: "u.kaya"),
            Po("P2", 60_000m, "2026-05-04T11:00", requester: "U.Kaya"));

        Assert.Single(new SplitOrderRule(Options).Evaluate(data));
    }

    // Self-approval

    [Fact]
    public void Self_approval_flags_requester_approving_own_order_ignoring_case()
    {
        var data = Orders(Po("P1", 5_000m, "2026-05-04T10:00", requester: "u.kaya", approver: "U.Kaya"));

        var finding = Assert.Single(new SelfApprovalRule().Evaluate(data));

        Assert.Equal(["P1"], finding.DocumentIds);
    }

    [Fact]
    public void Self_approval_ignores_orders_approved_by_someone_else()
    {
        var data = Orders(Po("P1", 5_000m, "2026-05-04T10:00"));

        Assert.Empty(new SelfApprovalRule().Evaluate(data));
    }

    // Off-hours approval (2026-05-02 is a Saturday, 2026-05-04 a Monday)

    [Theory]
    [InlineData("2026-05-02T10:00", true)]
    [InlineData("2026-05-04T06:59", true)]
    [InlineData("2026-05-04T20:00", true)]
    [InlineData("2026-05-04T07:00", false)]
    [InlineData("2026-05-04T19:59", false)]
    public void Off_hours_flags_weekend_and_out_of_workday_approvals(string approvedAt, bool flagged)
    {
        var data = Orders(Po("P1", 5_000m, approvedAt));

        var findings = new OffHoursApprovalRule(Options).Evaluate(data);

        Assert.Equal(flagged, findings.Any());
    }
}
