using GA_TroutStocking_Loader.Tests.TestHelpers;
using Xunit;

namespace GA_TroutStocking_Loader.Tests;

public sealed class Program_ParseConfigJsonTests
{
    [Fact]
    public void ParseConfigJson_WhenValid_ReturnsConfig()
    {
        var json = """
        {
          "PdfUrl": "https://example.com/report.pdf",
          "ConnectionStrings": {
            "Sql": "Server=.;Database=Db;Trusted_Connection=True"
          }
        }
        """;

        var config = LegacyJsonConfigParser.ParseConfigJson(json);

        Assert.Equal("https://example.com/report.pdf", config.PdfUrl);
        Assert.Equal("Server=.;Database=Db;Trusted_Connection=True", config.SqlConnectionString);
    }

    [Fact]
    public void ParseConfigJson_WhenMissingPdfUrl_ThrowsInvalidOperationException()
    {
        var json = """
        {
          "ConnectionStrings": {
            "Sql": "Server=.;Database=Db;Trusted_Connection=True"
          }
        }
        """;

        var ex = Assert.Throws<InvalidOperationException>(() => LegacyJsonConfigParser.ParseConfigJson(json));
        Assert.Equal("appsettings.json is missing PdfUrl or ConnectionStrings:Sql", ex.Message);
    }

    [Fact]
    public void ParseConfigJson_WhenMissingConnectionString_ThrowsInvalidOperationException()
    {
        var json = """
        {
          "PdfUrl": "https://example.com/report.pdf",
          "ConnectionStrings": {
            "NotSql": "x"
          }
        }
        """;

        var ex = Assert.Throws<InvalidOperationException>(() => LegacyJsonConfigParser.ParseConfigJson(json));
        Assert.Equal("appsettings.json is missing PdfUrl or ConnectionStrings:Sql", ex.Message);
    }

    [Fact]
    public void ParseConfigJson_WhenPdfUrlIsWhitespace_ThrowsInvalidOperationException()
    {
        var json = """
        {
          "PdfUrl": "   ",
          "ConnectionStrings": {
            "Sql": "Server=.;Database=Db;Trusted_Connection=True"
          }
        }
        """;

        var ex = Assert.Throws<InvalidOperationException>(() => LegacyJsonConfigParser.ParseConfigJson(json));
        Assert.Equal("appsettings.json is missing PdfUrl or ConnectionStrings:Sql", ex.Message);
    }
}