namespace GA_TroutStocking_Loader.Configuration;

internal sealed class AppOptions
{
    public string PdfUrl { get; set; } = string.Empty;

    /// <summary>
    /// Bind from ConnectionStrings:Sql
    /// </summary>
    public string SqlConnectionString { get; set; } = string.Empty;
}
