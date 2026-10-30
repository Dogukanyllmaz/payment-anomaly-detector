using System.Text.Json;
using System.Text.Json.Serialization;

namespace PaymentAnomalies;

public sealed record Invoice(
    string Id,
    string SupplierId,
    string InvoiceNumber,
    DateOnly Date,
    decimal Amount);

public sealed record PurchaseOrder(
    string Id,
    string RequesterId,
    string ApproverId,
    string SupplierId,
    decimal Amount,
    DateTime ApprovedAt);

/// <summary>Everything the rules look at in one run.</summary>
public sealed record AuditData(IReadOnlyList<Invoice> Invoices, IReadOnlyList<PurchaseOrder> Orders);

/// <summary>One suspicious item for an auditor to review: which rule fired, on which documents, and why.</summary>
public sealed record Finding(string Rule, IReadOnlyList<string> DocumentIds, string Reason);

/// <summary>Thresholds; loaded from rules.json in the data folder so they can be tuned without a rebuild.</summary>
public sealed record RuleOptions
{
    private const int MaxWindowDays = 366;

    public int DuplicateWindowDays { get; init; } = 7;
    public int SplitWindowDays { get; init; } = 3;
    public decimal ApprovalLimit { get; init; } = 100_000m;
    public TimeOnly WorkdayStart { get; init; } = new(7, 0);
    public TimeOnly WorkdayEnd { get; init; } = new(20, 0);

    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true,
        // a misspelt key must stop the run, not leave the audit on a default threshold
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public static RuleOptions FromJson(string json)
    {
        var options = JsonSerializer.Deserialize<RuleOptions>(json, Json)
                      ?? throw new InvalidOperationException("rules.json is empty.");
        if (options.WorkdayStart >= options.WorkdayEnd)
            throw new InvalidOperationException("WorkdayStart must be earlier than WorkdayEnd.");
        if (options.DuplicateWindowDays is < 0 or > MaxWindowDays || options.SplitWindowDays is < 0 or > MaxWindowDays)
            throw new InvalidOperationException($"Window days must be between 0 and {MaxWindowDays}.");
        if (options.ApprovalLimit <= 0)
            throw new InvalidOperationException("ApprovalLimit must be positive.");
        return options;
    }
}
