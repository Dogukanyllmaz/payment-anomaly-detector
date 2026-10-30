using System.Text.Json;

namespace PaymentAnomalies.Tests;

public class RuleOptionsTests
{
    [Fact]
    public void Reads_thresholds_regardless_of_key_case()
    {
        var options = RuleOptions.FromJson("""{ "approvalLimit": 50000, "WorkdayStart": "08:00:00" }""");

        Assert.Equal(50_000m, options.ApprovalLimit);
        Assert.Equal(new TimeOnly(8, 0), options.WorkdayStart);
    }

    [Fact]
    public void Rejects_an_unknown_key_instead_of_silently_using_the_default()
    {
        Assert.Throws<JsonException>(() => RuleOptions.FromJson("""{ "SplitWindowDay": 5 }"""));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("""{ "SplitWindowDays": -1 }""")]
    [InlineData("""{ "DuplicateWindowDays": 2147483647 }""")]
    [InlineData("""{ "SplitWindowDays": 367 }""")]
    [InlineData("""{ "ApprovalLimit": 0 }""")]
    public void Rejects_thresholds_that_would_make_the_audit_meaningless_or_crash(string json)
    {
        Assert.Throws<InvalidOperationException>(() => RuleOptions.FromJson(json));
    }

    [Theory]
    [InlineData("20:00:00", "07:00:00")]
    [InlineData("09:00:00", "09:00:00")]
    public void Rejects_a_workday_that_does_not_start_before_it_ends(string start, string end)
    {
        var json = $$"""{ "WorkdayStart": "{{start}}", "WorkdayEnd": "{{end}}" }""";

        Assert.Throws<InvalidOperationException>(() => RuleOptions.FromJson(json));
    }
}
