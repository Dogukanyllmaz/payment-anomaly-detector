using PaymentAnomalies.Cli;

namespace PaymentAnomalies.Tests;

/// <summary>End-to-end: arguments in, exit code, console text and report file out.</summary>
public sealed class AppTests : IDisposable
{
    private const string Rules = """{ "ApprovalLimit": 100000 }""";

    private readonly string _folder = Directory.CreateTempSubdirectory("anomalies-").FullName;
    private readonly StringWriter _out = new();
    private readonly StringWriter _err = new();

    public void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
        _out.Dispose();
        _err.Dispose();
    }

    private string Report => Path.Combine(_folder, "report.csv");

    private int Run(params string[] args) => App.Run(args, _out, _err);

    private void Write(string rules, string[] invoices, string[] orders)
    {
        File.WriteAllText(Path.Combine(_folder, "rules.json"), rules);
        File.WriteAllLines(Path.Combine(_folder, "invoices.csv"), [CsvLoader.InvoiceHeader, .. invoices]);
        File.WriteAllLines(Path.Combine(_folder, "orders.csv"), [CsvLoader.OrderHeader, .. orders]);
    }

    [Fact]
    public void Sample_data_yields_exactly_the_five_seeded_findings_and_exit_code_1()
    {
        var sample = Path.Combine(AppContext.BaseDirectory, "sample");

        var code = Run(sample, Report);

        Assert.Equal(App.FindingsFound, code);
        Assert.Contains("5 finding(s)", _out.ToString());
        Assert.Equal(6, File.ReadAllLines(Report).Length); // header + 5
    }

    [Fact]
    public void Clean_data_exits_0_and_still_writes_an_empty_report()
    {
        Write(Rules, ["I1,SUP-1,A-1,2026-05-04,100"], ["P1,u.kaya,m.demir,SUP-1,100,2026-05-04T10:00"]);

        var code = Run(_folder, Report);

        Assert.Equal(App.NoFindings, code);
        Assert.Equal(["Rule,Documents,Reason"], File.ReadAllLines(Report));
    }

    [Fact]
    public void Missing_folder_exits_2_with_a_one_line_message_and_no_stack_trace()
    {
        var code = Run(Path.Combine(_folder, "nope"), Report);

        Assert.Equal(App.Failed, code);
        Assert.StartsWith("Error:", _err.ToString());
        Assert.DoesNotContain("   at ", _err.ToString());
    }

    [Fact]
    public void Bad_csv_line_exits_2_naming_the_file_and_line()
    {
        Write(Rules, [], ["P1,u.kaya,m.demir,SUP-1,abc,2026-05-04T10:00"]);

        var code = Run(_folder, Report);

        Assert.Equal(App.Failed, code);
        Assert.Contains("orders.csv: Line 2:", _err.ToString());
    }

    [Fact]
    public void Misspelt_rules_key_exits_2()
    {
        Write("""{ "ApprovalLimt": 100000 }""", [], []);

        Assert.Equal(App.Failed, Run(_folder, Report));
        Assert.StartsWith("Error:", _err.ToString());
    }

    [Fact]
    public void Too_many_arguments_exits_2_with_usage()
    {
        Assert.Equal(App.Failed, Run("a", "b", "c"));
        Assert.Contains("Usage:", _err.ToString());
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("-h")]
    public void Help_prints_usage_and_exits_0(string flag)
    {
        Assert.Equal(App.NoFindings, Run(flag));
        Assert.Contains("Usage:", _out.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("bad\0name.csv")]
    public void Invalid_report_path_exits_2_without_a_stack_trace(string reportPath)
    {
        Write(Rules, [], []);

        Assert.Equal(App.Failed, Run(_folder, reportPath));
        Assert.StartsWith("Error:", _err.ToString());
    }

    [Fact]
    public void Amounts_that_overflow_when_added_exit_2_without_a_stack_trace()
    {
        const string huge = "50000000000000000000000000000";
        Write("""{ "ApprovalLimit": 79000000000000000000000000000 }""", [],
        [
            $"P1,u.kaya,m.demir,SUP-1,{huge},2026-05-04T10:00",
            $"P2,u.kaya,m.demir,SUP-1,{huge},2026-05-04T11:00"
        ]);

        Assert.Equal(App.Failed, Run(_folder, Report));
        Assert.StartsWith("Error:", _err.ToString());
    }

    [Fact]
    public void Failed_run_removes_the_previous_report_so_ci_never_reads_stale_findings()
    {
        File.WriteAllText(Report, "stale");
        Write(Rules, [], ["P1,u.kaya,m.demir,SUP-1,abc,2026-05-04T10:00"]);

        Assert.Equal(App.Failed, Run(_folder, Report));
        Assert.False(File.Exists(Report));
    }
}
