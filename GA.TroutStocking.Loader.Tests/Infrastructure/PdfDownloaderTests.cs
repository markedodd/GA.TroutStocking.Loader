using GA_TroutStocking_Loader.Infrastructure;
using GA_TroutStocking_Loader.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Sockets;
using Xunit;

namespace GA_TroutStocking_Loader.Tests.Infrastructure;

public sealed class PdfDownloaderTests
{
    [Fact]
    public async Task DownloadAsync_WhenResponseIsPdfHeader_WritesTempFileAndReturnsPath()
    {
        using var server = new TestHttpServer(context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/pdf";

            var bytes = new byte[] { (byte)'%', (byte)'P', (byte)'D', (byte)'F', (byte)'-', (byte)'1', (byte)'.', (byte)'7', (byte)'\n' };
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Flush();
            context.Response.Close();
        });

        using var provider = CreateProvider();
        var sut = provider.GetRequiredService<IPdfDownloader>();

        var tempFilePath = await sut.DownloadAsync(server.Url);

        try
        {
            Assert.False(string.IsNullOrWhiteSpace(tempFilePath));
            Assert.True(File.Exists(tempFilePath));

            var savedBytes = await File.ReadAllBytesAsync(tempFilePath);

            Assert.True(savedBytes.Length >= 4);
            Assert.Equal((byte)'%', savedBytes[0]);
            Assert.Equal((byte)'P', savedBytes[1]);
            Assert.Equal((byte)'D', savedBytes[2]);
            Assert.Equal((byte)'F', savedBytes[3]);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(tempFilePath) && File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public async Task DownloadAsync_WhenResponseIsNotPdf_ThrowsInvalidOperationException()
    {
        using var server = new TestHttpServer(context =>
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "text/plain";

            var bytes = "Not a PDF"u8.ToArray();
            context.Response.OutputStream.Write(bytes, 0, bytes.Length);
            context.Response.OutputStream.Flush();
            context.Response.Close();
        });

        using var provider = CreateProvider();
        var sut = provider.GetRequiredService<IPdfDownloader>();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.DownloadAsync(server.Url));

        Assert.Equal("Downloaded content does not appear to be a PDF.", ex.Message);
    }

    private static ServiceProvider CreateProvider()
    {
        // Minimal DI setup to exercise the real IHttpClientFactory (typed client) behavior.
        var services = new ServiceCollection();

        services.AddHttpClient<IPdfDownloader, PdfDownloader>(c =>
            {
                c.DefaultRequestHeaders.UserAgent.ParseAdd("GaTroutStockingLoader/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

        return services.BuildServiceProvider();
    }

    private sealed class TestHttpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly CancellationTokenSource _cts = new();

        public string Url { get; }

        public TestHttpServer(Action<HttpListenerContext> handle)
        {
            var port = GetFreePort();
            Url = $"http://127.0.0.1:{port}/report.pdf";

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();

            _ = Task.Run(async () =>
            {
                while (!_cts.IsCancellationRequested)
                {
                    HttpListenerContext? context = null;
                    try
                    {
                        context = await _listener.GetContextAsync();
                        handle(context);
                    }
                    catch (ObjectDisposedException)
                    {
                        return;
                    }
                    catch (HttpListenerException)
                    {
                        return;
                    }
                    catch
                    {
                        try { context?.Response.Abort(); } catch { }
                        throw;
                    }
                }
            }, _cts.Token);
        }

        public void Dispose()
        {
            _cts.Cancel();

            if (_listener.IsListening)
            {
                _listener.Stop();
            }

            _listener.Close();
            _cts.Dispose();
        }

        private static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}
