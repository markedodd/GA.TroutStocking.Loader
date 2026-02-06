using GA_TroutStocking_Loader.Services.Interfaces;

namespace GA_TroutStocking_Loader.Infrastructure;

internal sealed class PdfDownloader(HttpClient httpClient) : IPdfDownloader
{
    public async Task<string> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await httpClient.GetByteArrayAsync(url, cancellationToken);

            if (bytes.Length < 4 || bytes[0] != (byte)'%' || bytes[1] != (byte)'P' || bytes[2] != (byte)'D' || bytes[3] != (byte)'F')
            {
                throw new InvalidOperationException("Downloaded content does not appear to be a PDF.");
            }

            var tempFile = Path.GetTempFileName();
            await File.WriteAllBytesAsync(tempFile, bytes, cancellationToken);

            return tempFile;
        }
        catch (Exception ex)
        {
            log4net.ThreadContext.Properties["jsonException"] = LogJson.Exception(ex);
            Log.Logger.Error(LogJson.Message("Failed to download PDF", new { url }), ex);
            log4net.ThreadContext.Properties.Remove("jsonException");

            throw;
        }
    }
}
