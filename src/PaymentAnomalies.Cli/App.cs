using System.Text.Json;

namespace PaymentAnomalies.Cli;

/// <summary>
/// Command-line entry. The exit code lets a scheduler or CI pipeline act on the result
/// without parsing the console output.
/// </summary>
internal static class App
{
    public const int NoFindings = 0;
    public const int FindingsFound = 1;
    public const int Failed = 2; // bad arguments, input, configuration or report path

    private const string Usage =
        "Usage: paymentanomalies [dataFolder=data] [reportPath=findings.csv]\n" +
        "Exit codes: 0 no findings, 1 findings written to the report, 2 error (nothing written)";

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args is ["-h"] or ["--help"])
        {
            output.WriteLine(Usage);
            return NoFindings;
        }
        if (args.Length > 2)
        {
            error.WriteLine(Usage);
            return Failed;
        }

        var dataFolder = args.Length > 0 ? args[0] : "data";
        var reportPath = args.Length > 1 ? args[1] : "findings.csv";

        try
        {
            File.Delete(reportPath); // a failed run must not leave the previous run's findings behind
            var options = RuleOptions.FromJson(File.ReadAllText(Path.Combine(dataFolder, "rules.json")));
            var data = new AuditData(
                Load(Path.Combine(dataFolder, "invoices.csv"), CsvLoader.ParseInvoices),
                Load(Path.Combine(dataFolder, "orders.csv"), CsvLoader.ParseOrders));

            var findings = Audit.Run(data, options);
            ReportWriter.WriteCsv(reportPath, findings);
            Print(output, data, findings, reportPath);
            return findings.Count == 0 ? NoFindings : FindingsFound;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException
                                       or NotSupportedException or FormatException or JsonException
                                       or InvalidOperationException or OverflowException)
        {
            error.WriteLine($"Error: {ex.Message}");
            return Failed;
        }
    }

    private static T Load<T>(string path, Func<IEnumerable<string>, T> parse)
    {
        try
        {
            return parse(File.ReadLines(path));
        }
        catch (FormatException ex)
        {
            throw new FormatException($"{Path.GetFileName(path)}: {ex.Message}", ex);
        }
    }

    private static void Print(TextWriter output, AuditData data, IReadOnlyList<Finding> findings, string reportPath)
    {
        foreach (var finding in findings)
            output.WriteLine($"[{finding.Rule}] {string.Join(", ", finding.DocumentIds)}: {finding.Reason}");

        output.WriteLine();
        output.WriteLine($"Checked {data.Invoices.Count} invoices and {data.Orders.Count} orders: {findings.Count} finding(s)");
        foreach (var group in findings.GroupBy(f => f.Rule))
            output.WriteLine($"  {group.Key}: {group.Count()}");
        output.WriteLine($"Report written to {reportPath}");
    }
}
