using System.Text;

namespace PaymentAnomalies;

/// <summary>Writes findings as a CSV an auditor can open in a spreadsheet.</summary>
public static class ReportWriter
{
    // BOM so spreadsheets show non-ASCII names (ş, ç, ğ) correctly
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    public static void WriteCsv(string path, IEnumerable<Finding> findings) =>
        File.WriteAllLines(path, ToCsvLines(findings), Utf8WithBom);

    public static IEnumerable<string> ToCsvLines(IEnumerable<Finding> findings) =>
    [
        "Rule,Documents,Reason",
        .. findings.Select(f => string.Join(",", Quote(f.Rule), Quote(string.Join(" ", f.DocumentIds)), Quote(f.Reason)))
    ];

    // Values come from the input files; a leading = + - @ would run as a formula when the report is opened.
    private static string Quote(string value)
    {
        var safe = value.Length > 0 && "=+-@\t\r".Contains(value[0]) ? "'" + value : value;
        return $"\"{safe.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }
}
