using System.Reflection;
using Xunit;

namespace GA_TroutStocking_Loader.Tests;

public sealed class Program_ParseConfigJsonTests
{
    private static object InvokeParseConfigJson(string json)
    {
        var programType = typeof(GA_TroutStocking_Loader.Program);

        var method = programType.GetMethod(
            "ParseConfigJson",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return method!.Invoke(null, new object?[] { json })!;
    }

    private static string GetConfigProperty(object config, string propertyName)
    {
        var prop = config.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(prop);

        return (string)prop!.GetValue(config)!;
    }

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

        var config = InvokeParseConfigJson(json);

        Assert.Equal("https://example.com/report.pdf", GetConfigProperty(config, "PdfUrl"));
        Assert.Equal("Server=.;Database=Db;Trusted_Connection=True", GetConfigProperty(config, "SqlConnectionString"));
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

        var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseConfigJson(json));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("appsettings.json is missing PdfUrl or ConnectionStrings:Sql", ex.InnerException!.Message);
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

        var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseConfigJson(json));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("appsettings.json is missing PdfUrl or ConnectionStrings:Sql", ex.InnerException!.Message);
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

        var ex = Assert.Throws<TargetInvocationException>(() => InvokeParseConfigJson(json));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal("appsettings.json is missing PdfUrl or ConnectionStrings:Sql", ex.InnerException!.Message);
    }
}