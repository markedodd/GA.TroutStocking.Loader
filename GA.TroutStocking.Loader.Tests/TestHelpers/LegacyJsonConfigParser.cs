using System.Text.RegularExpressions;

namespace GA_TroutStocking_Loader.Tests.TestHelpers;

/// <summary>
/// Legacy config parsing preserved for unit tests.
/// The application runtime config uses the Generic Host configuration + Options.
/// </summary>
internal static class LegacyJsonConfigParser
{
    internal static AppConfig ParseConfigJson(string json)
    {
        var pdfUrl = ReadJsonValue(json, "PdfUrl");
        var connStr = ReadJsonValue(json, "Sql", "ConnectionStrings");

        if (string.IsNullOrWhiteSpace(pdfUrl) || string.IsNullOrWhiteSpace(connStr))
        {
            throw new InvalidOperationException("appsettings.json is missing PdfUrl or ConnectionStrings:Sql");
        }

        return new AppConfig(pdfUrl, connStr);
    }

    internal static string? ReadJsonValue(string json, string key, string? parentKey = null)
    {
        string pattern = parentKey == null
            ? $@"""{Regex.Escape(key)}""\s*:\s*""(?<v>[^""]+)"""
            : $@"""{Regex.Escape(parentKey)}""\s*:\s*\{{[\s\S]*?""{Regex.Escape(key)}""\s*:\s*""(?<v>[^""]+)""[\s\S]*?\}}";

        var m = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
        return m.Success ? m.Groups["v"].Value : null;
    }
}
