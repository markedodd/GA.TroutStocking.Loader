namespace GA_TroutStocking_Loader.Services.Interfaces;

internal interface IPdfDownloader
{
    Task<string> DownloadAsync(string url, CancellationToken cancellationToken = default);
}
