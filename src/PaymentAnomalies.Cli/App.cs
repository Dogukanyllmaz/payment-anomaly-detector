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
        "Usage: paymentanomalies [dataFolder=data] [reportPath=findings.csv] [--json jsonPath]\n" +
        "Exit codes: 0 no findings, 1 findings written to the report, 2 error (nothing written)";

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args is ["-h"] or ["--help"])
        {
            output.WriteLine(Usage);
            return NoFindings;
        }

        string? jsonPath = null;
        var jsonFlag = Array.IndexOf(args, "--json");
        if (jsonFlag >= 0)
        {
            if (jsonFlag == args.Length - 1 || args[jsonFlag + 1].StartsWith('-'))
            {
                error.WriteLine(Usage);
                return Failed;
            }
            jsonPath = args[jsonFlag + 1];
            args = [.. args[..jsonFlag], .. args[(jsonFlag + 2)..]];
        }

        if (args.Length > 2 || args.Contains("--json"))
        {
            error.WriteLine(Usage);
            return Failed;
        }

        var dataFolder = args.Length > 0 ? args[0] : "data";
        var reportPath = args.Length > 1 ? args[1] : "findings.csv";
        var rulesPath = Path.Combine(dataFolder, "rules.json");
        var invoicesPath = Path.Combine(dataFolder, "invoices.csv");
        var ordersPath = Path.Combine(dataFolder, "orders.csv");

        // the outputs are deleted before the run, so one pointing at an input or at the other output would destroy it
        string[] outputs = jsonPath is null ? [reportPath] : [reportPath, jsonPath];
        string[] taken = [rulesPath, invoicesPath, ordersPath];
        if (outputs.Any(o => taken.Any(t => SamePath(o, t))) || (jsonPath is not null && SamePath(reportPath, jsonPath)))
        {
            error.WriteLine("Error: the report paths must not point at an input file or at each other.");
            return Failed;
        }

        try
        {
            // a failed run must not leave the previous run's findings behind
            File.Delete(reportPath);
            if (jsonPath is not null)
                File.Delete(jsonPath);
            var options = RuleOptions.FromJson(File.ReadAllText(rulesPath));
            var data = new AuditData(
                Load(invoicesPath, CsvLoader.ParseInvoices),
                Load(ordersPath, CsvLoader.ParseOrders));

            var findings = Audit.Run(data, options);
            // JSON first: if it cannot be written, no CSV is left behind to contradict exit code 2
            if (jsonPath is not null)
                File.WriteAllText(jsonPath, JsonReport.Serialize(data, options, findings));
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

    private static bool SamePath(string a, string b)
    {
        try
        {
            return string.Equals(Path.GetFullPath(a), Path.GetFullPath(b), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or IOException)
        {
            return false; // an unusable path fails with a clear message when the run opens it
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
