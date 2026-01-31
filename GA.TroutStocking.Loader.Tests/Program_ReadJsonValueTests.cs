using System.Reflection;
using Xunit;

namespace GA_TroutStocking_Loader.Tests;

public sealed class Program_ReadJsonValueTests
{
    private static string? InvokeReadJsonValue(string json, string key, string? parentKey = null)
    {
        var programType = typeof(GA_TroutStocking_Loader.Program);

        var method = programType.GetMethod(
            "ReadJsonValue",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return (string?)method!.Invoke(null, new object?[] { json, key, parentKey });
    }

    [Fact]
    public void ReadJsonValue_TopLevelKey_ReturnsValue()
    {
        var json = """
        {
          "PdfUrl": "https://example.com/report.pdf"
        }
        """;

        var value = InvokeReadJsonValue(json, "PdfUrl");

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

        var value = InvokeReadJsonValue(json, "PdfUrl");

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

        var value = InvokeReadJsonValue(json, "PdfUrl");

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

        var value = InvokeReadJsonValue(json, "Sql", "ConnectionStrings");

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

        var value = InvokeReadJsonValue(json, "Sql", "ConnectionStrings");

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

        var value = InvokeReadJsonValue(json, "Sql", "ConnectionStrings");

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

        var value = InvokeReadJsonValue(json, "PdfUrl");

        Assert.Equal("https://example.com/a b/report.pdf", value);
    }
}