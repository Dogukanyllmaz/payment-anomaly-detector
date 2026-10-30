using System.Globalization;

namespace PaymentAnomalies;

/// <summary>Reads the exported CSV files. The first line must be the header; values use invariant culture.</summary>
public static class CsvLoader
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public const string InvoiceHeader = "Id,SupplierId,InvoiceNumber,Date,Amount";
    public const string OrderHeader = "Id,RequesterId,ApproverId,SupplierId,Amount,ApprovedAt";

    public static IReadOnlyList<Invoice> ParseInvoices(IEnumerable<string> lines) =>
        Parse(lines, InvoiceHeader, f => new Invoice(
            f[0], f[1], f[2],
            DateOnly.ParseExact(f[3], "yyyy-MM-dd", Culture),
            decimal.Parse(f[4], NumberStyles.Number, Culture)));

    public static IReadOnlyList<PurchaseOrder> ParseOrders(IEnumerable<string> lines) =>
        Parse(lines, OrderHeader, f => new PurchaseOrder(
            f[0], f[1], f[2], f[3],
            decimal.Parse(f[4], NumberStyles.Number, Culture),
            DateTime.ParseExact(f[5], "yyyy-MM-ddTHH:mm", Culture)));

    private static List<T> Parse<T>(IEnumerable<string> lines, string header, Func<string[], T> map)
    {
        var fieldCount = header.Split(',').Length;
        var result = new List<T>();
        var lineNumber = 0;
        foreach (var line in lines)
        {
            lineNumber++;
            if (lineNumber == 1)
            {
                // a headerless export would otherwise lose its first record without a word
                if (!string.Equals(line.Trim(), header, StringComparison.OrdinalIgnoreCase))
                    throw new FormatException($"Line 1: expected header \"{header}\".");
                continue;
            }
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = line.Split(',').Select(f => f.Trim()).ToArray();
            if (fields.Length != fieldCount)
                throw new FormatException($"Line {lineNumber}: expected {fieldCount} fields, found {fields.Length}.");
            if (Array.IndexOf(fields, "") is var empty and >= 0)
                throw new FormatException($"Line {lineNumber}: {header.Split(',')[empty]} is empty.");

            try
            {
                result.Add(map(fields));
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw new FormatException($"Line {lineNumber}: {ex.Message}", ex);
            }
        }
        return result;
    }
}
