using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaymentAnomalies;

/// <summary>
/// The audit result as JSON for the web demo: what was checked, the thresholds used and every finding
/// with the documents behind it, so a reader can see the evidence without opening the CSV files.
/// </summary>
public static class JsonReport
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(AuditData data, RuleOptions options, IReadOnlyList<Finding> findings)
    {
        var invoices = data.Invoices.GroupBy(i => i.Id).ToDictionary(g => g.Key, g => g.First());
        var orders = data.Orders.GroupBy(o => o.Id).ToDictionary(g => g.Key, g => g.First());

        // duplicate-invoice findings point at invoices; every other rule points at orders
        Document ToDocument(Finding finding, string id) =>
            finding.Rule == DuplicateInvoiceRule.Name ? FromInvoice(invoices[id]) : FromOrder(orders[id]);

        var report = new Report(
            new Summary(data.Invoices.Count, data.Orders.Count, findings.Count),
            new Thresholds(options.DuplicateWindowDays, options.SplitWindowDays, options.ApprovalLimit,
                options.WorkdayStart.ToString("HH:mm", Culture), options.WorkdayEnd.ToString("HH:mm", Culture)),
            findings.Select(f => new FindingView(f.Rule, f.Reason, f.DocumentIds.Select(id => ToDocument(f, id)).ToList()))
                .ToList());
        return JsonSerializer.Serialize(report, Json);
    }

    private static Document FromInvoice(Invoice i) =>
        new("invoice", i.Id, i.SupplierId, i.Amount,
            InvoiceNumber: i.InvoiceNumber, Date: i.Date.ToString("yyyy-MM-dd", Culture));

    private static Document FromOrder(PurchaseOrder o) =>
        new("order", o.Id, o.SupplierId, o.Amount,
            RequesterId: o.RequesterId, ApproverId: o.ApproverId,
            ApprovedAt: o.ApprovedAt.ToString("yyyy-MM-ddTHH:mm", Culture));

    private sealed record Report(Summary Summary, Thresholds Thresholds, IReadOnlyList<FindingView> Findings);

    private sealed record Summary(int Invoices, int Orders, int Findings);

    private sealed record Thresholds(
        int DuplicateWindowDays, int SplitWindowDays, decimal ApprovalLimit, string WorkdayStart, string WorkdayEnd);

    private sealed record FindingView(string Rule, string Reason, IReadOnlyList<Document> Documents);

    private sealed record Document(
        string Kind, string Id, string SupplierId, decimal Amount,
        string? InvoiceNumber = null, string? Date = null,
        string? RequesterId = null, string? ApproverId = null, string? ApprovedAt = null);
}
