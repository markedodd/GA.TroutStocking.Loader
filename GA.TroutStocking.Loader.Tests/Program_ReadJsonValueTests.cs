using GA_TroutStocking_Loader.Tests.TestHelpers;
using Xunit;

namespace GA_TroutStocking_Loader.Tests;

public sealed class Program_ReadJsonValueTests
{
    [Fact]
    public void ReadJsonValue_TopLevelKey_ReturnsValue()
    {
        var json = """
        {
          "PdfUrl": "https://example.com/report.pdf"
        }
        """;

        var value = LegacyJsonConfigParser.ReadJsonValue(json, "PdfUrl");

        Assert.Equal("https://example.com/report.pdf", value);
    }

    [Fact]
    public void ReadJsonValue_TopLevelKey_Missing_ReturnsNull()
    {
        var json = """
        {
          "Other": "x"
        }
        """;

        var value = LegacyJsonConfigParser.ReadJsonValue(json, "PdfUrl");

        Assert.Null(value);
    }

    [Fact]
    public void ReadJsonValue_IsCaseInsensitive()
    {
        var json = """
        {
          "pdfurl": "https://example.com/report.pdf"
        }
        """;

        var value = LegacyJsonConfigParser.ReadJsonValue(json, "PdfUrl");

        Assert.Equal("https://example.com/report.pdf", value);
    }

    [Fact]
    public void ReadJsonValue_ParentKey_ReturnsNestedValue()
    {
        var json = """
        {
          "ConnectionStrings": {
            "Sql": "Server=.;Database=Db;Trusted_Connection=True"
          }
        }
        """;

        var value = LegacyJsonConfigParser.ReadJsonValue(json, "Sql", "ConnectionStrings");

        Assert.Equal("Server=.;Database=Db;Trusted_Connection=True", value);
    }

    [Fact]
    public void ReadJsonValue_ParentKey_MissingParent_ReturnsNull()
    {
        var json = """
        {
          "Other": {
            "Sql": "Server=.;Database=Db;Trusted_Connection=True"
          }
        }
        """;

        var value = LegacyJsonConfigParser.ReadJsonValue(json, "Sql", "ConnectionStrings");

        Assert.Null(value);
    }

    [Fact]
    public void ReadJsonValue_ParentKey_MissingChild_ReturnsNull()
    {
        var json = """
        {
          "ConnectionStrings": {
            "NotSql": "x"
          }
        }
        """;

        var value = LegacyJsonConfigParser.ReadJsonValue(json, "Sql", "ConnectionStrings");

        Assert.Null(value);
    }

    [Fact]
    public void ReadJsonValue_WhenValueContainsSpaces_ReturnsEntireValue()
    {
        var json = """
        {
          "PdfUrl": "https://example.com/a b/report.pdf"
        }
        """;

        var value = LegacyJsonConfigParser.ReadJsonValue(json, "PdfUrl");

        Assert.Equal("https://example.com/a b/report.pdf", value);
    }
}