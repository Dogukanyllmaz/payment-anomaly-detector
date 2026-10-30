using System.Text.Json;

namespace PaymentAnomalies.Tests;

public class JsonReportTests
{
    private static readonly Invoice I1 = new("I1", "SUP-1", "A-1", new DateOnly(2026, 5, 1), 1500m);
    private static readonly Invoice I2 = new("I2", "SUP-1", "A-2", new DateOnly(2026, 5, 3), 1500m);
    private static readonly PurchaseOrder P1 = new("P1", "u.kaya", "U.Kaya", "SUP-2", 800m, new DateTime(2026, 5, 4, 10, 30, 0));

    private static readonly AuditData Data = new([I1, I2], [P1]);

    private static JsonElement Render()
    {
        var options = new RuleOptions();
        var json = JsonReport.Serialize(Data, options, Audit.Run(Data, options));
        return JsonDocument.Parse(json).RootElement;
    }

    [Fact]
    public void Summarises_what_was_checked_and_the_thresholds_used()
    {
        var root = Render();

        Assert.Equal(2, root.GetProperty("summary").GetProperty("invoices").GetInt32());
        Assert.Equal(1, root.GetProperty("summary").GetProperty("orders").GetInt32());
        Assert.Equal(2, root.GetProperty("summary").GetProperty("findings").GetInt32());
        Assert.Equal(100_000m, root.GetProperty("thresholds").GetProperty("approvalLimit").GetDecimal());
        Assert.Equal("07:00", root.GetProperty("thresholds").GetProperty("workdayStart").GetString());
    }

    [Fact]
    public void Duplicate_invoice_findings_carry_the_invoices_themselves()
    {
        var finding = Render().GetProperty("findings")[0];

        Assert.Equal(DuplicateInvoiceRule.Name, finding.GetProperty("rule").GetString());
        var document = finding.GetProperty("documents")[1];
        Assert.Equal("invoice", document.GetProperty("kind").GetString());
        Assert.Equal("I2", document.GetProperty("id").GetString());
        Assert.Equal("A-2", document.GetProperty("invoiceNumber").GetString());
        Assert.Equal("2026-05-03", document.GetProperty("date").GetString());
        Assert.Equal(1500m, document.GetProperty("amount").GetDecimal());
        Assert.False(document.TryGetProperty("requesterId", out _));
    }

    [Fact]
    public void Order_findings_carry_requester_approver_and_approval_time()
    {
        var finding = Render().GetProperty("findings")[1];

        Assert.Equal(SelfApprovalRule.Name, finding.GetProperty("rule").GetString());
        var document = finding.GetProperty("documents")[0];
        Assert.Equal("order", document.GetProperty("kind").GetString());
        Assert.Equal("u.kaya", document.GetProperty("requesterId").GetString());
        Assert.Equal("U.Kaya", document.GetProperty("approverId").GetString());
        Assert.Equal("2026-05-04T10:30", document.GetProperty("approvedAt").GetString());
        Assert.False(document.TryGetProperty("invoiceNumber", out _));
    }
}
