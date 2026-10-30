namespace PaymentAnomalies.Tests;

public class ReportWriterTests
{
    private static Finding Finding(string reason) => new("Self-approval", ["P1", "P2"], reason);

    [Fact]
    public void Writes_a_header_and_one_quoted_row_per_finding()
    {
        var lines = ReportWriter.ToCsvLines([Finding("u.kaya approved their own order")]).ToList();

        Assert.Equal(
            ["Rule,Documents,Reason", "\"Self-approval\",\"P1 P2\",\"u.kaya approved their own order\""],
            lines);
    }

    [Fact]
    public void Doubles_quotes_inside_values()
    {
        var row = ReportWriter.ToCsvLines([Finding("say \"hi\"")]).Last();

        Assert.EndsWith(",\"say \"\"hi\"\"\"", row);
    }

    [Theory]
    [InlineData("=HYPERLINK(1)")]
    [InlineData("+1")]
    [InlineData("-1")]
    [InlineData("@SUM(A1)")]
    [InlineData("\tx")]
    public void Neutralises_values_a_spreadsheet_would_run_as_formulas(string reason)
    {
        var row = ReportWriter.ToCsvLines([Finding(reason)]).Last();

        Assert.EndsWith($",\"'{reason}\"", row);
    }

    [Fact]
    public void Writes_the_file_with_a_utf8_bom_so_spreadsheets_show_turkish_names()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
        try
        {
            ReportWriter.WriteCsv(path, [Finding("ş.çelik approved their own order")]);

            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, File.ReadAllBytes(path)[..3]);
            Assert.Contains("ş.çelik", File.ReadAllText(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
