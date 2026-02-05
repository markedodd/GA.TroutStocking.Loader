using System.Net;
using System.Reflection;
using Xunit;

namespace GA_TroutStocking_Loader.Tests;

public sealed class Program_DownloadPdfAsyncTests
{
    private static async Task<string> InvokeDownloadPdfAsync(string url)
    {
        var programType = typeof(GA_TroutStocking_Loader.Program);

        var method = programType.GetMethod(
            "DownloadPdfAsync",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var task = (Task<string>)method!.Invoke(null, new object?[] { url })!;
        return await task;
    }

    [Fact]
    public async Task DownloadPdfAsync_WhenResponseIsPdfHeader_WritesTempFileAndReturnsPath()
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

        var tempFilePath = await InvokeDownloadPdfAsync(server.Url);

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
    public async Task DownloadPdfAsync_WhenResponseIsNotPdf_ThrowsInvalidOperationException()
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

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => InvokeDownloadPdfAsync(server.Url));

        Assert.Equal("Downloaded content does not appear to be a PDF.", ex.Message);
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
            var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}