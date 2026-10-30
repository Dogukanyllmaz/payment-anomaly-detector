namespace PaymentAnomalies.Tests;

public class CsvLoaderTests
{
    [Fact]
    public void Rejects_a_file_without_the_expected_header_instead_of_dropping_its_first_record()
    {
        var error = Assert.Throws<FormatException>(() => CsvLoader.ParseInvoices(
        [
            "I1,SUP-1,A-100,2026-05-01,1500"
        ]));

        Assert.StartsWith("Line 1:", error.Message);
    }

    [Fact]
    public void Rejects_an_empty_field()
    {
        var error = Assert.Throws<FormatException>(() => CsvLoader.ParseOrders(
        [
            "Id,RequesterId,ApproverId,SupplierId,Amount,ApprovedAt",
            "P1,,,SUP-1,100,2026-05-04T10:00"
        ]));

        Assert.StartsWith("Line 2:", error.Message);
    }

    [Fact]
    public void Parses_invoices_and_skips_the_header()
    {
        var invoices = CsvLoader.ParseInvoices(
        [
            "Id,SupplierId,InvoiceNumber,Date,Amount",
            "I1,SUP-1,A-100,2026-05-01,1500.50"
        ]);

        Assert.Equal(new Invoice("I1", "SUP-1", "A-100", new DateOnly(2026, 5, 1), 1500.50m), Assert.Single(invoices));
    }

    [Fact]
    public void Parses_orders()
    {
        var orders = CsvLoader.ParseOrders(
        [
            "Id,RequesterId,ApproverId,SupplierId,Amount,ApprovedAt",
            "P1,u.kaya,m.demir,SUP-1,40000,2026-05-04T10:30"
        ]);

        Assert.Equal(
            new PurchaseOrder("P1", "u.kaya", "m.demir", "SUP-1", 40000m, new DateTime(2026, 5, 4, 10, 30, 0)),
            Assert.Single(orders));
    }

    [Fact]
    public void Reports_the_line_number_of_an_invalid_value()
    {
        var error = Assert.Throws<FormatException>(() => CsvLoader.ParseInvoices(
        [
            "Id,SupplierId,InvoiceNumber,Date,Amount",
            "I1,SUP-1,A-100,2026-05-01,1500",
            "I2,SUP-1,A-101,2026-05-02,abc"
        ]));

        Assert.StartsWith("Line 3:", error.Message);
    }

    [Fact]
    public void Reports_the_line_number_of_an_amount_too_large_to_hold()
    {
        var error = Assert.Throws<FormatException>(() => CsvLoader.ParseInvoices(
        [
            "Id,SupplierId,InvoiceNumber,Date,Amount",
            "I1,SUP-1,A-100,2026-05-01,99999999999999999999999999999999"
        ]));

        Assert.StartsWith("Line 2:", error.Message);
    }

    [Fact]
    public void Reports_a_line_with_the_wrong_number_of_fields()
    {
        var error = Assert.Throws<FormatException>(() => CsvLoader.ParseInvoices(
        [
            "Id,SupplierId,InvoiceNumber,Date,Amount",
            "I1,SUP-1,2026-05-01,1500"
        ]));

        Assert.StartsWith("Line 2:", error.Message);
    }
}
