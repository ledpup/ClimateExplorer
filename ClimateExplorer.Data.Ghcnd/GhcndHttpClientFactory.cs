namespace ClimateExplorer.Data.Ghcnd;

using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;

public static class GhcndHttpClientFactory
{
    public static HttpClient CreateHttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            ConnectCallback = ConnectToIpv4Async,
            ConnectTimeout = TimeSpan.FromSeconds(15),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        };

        var httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(60),
        };

        var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/97.0.4692.71 Safari/537.36";
        var acceptLanguage = "en-US,en;q=0.9,es;q=0.8";
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        httpClient.DefaultRequestHeaders.AcceptLanguage.ParseAdd(acceptLanguage);
        return httpClient;
    }

    private static async ValueTask<Stream> ConnectToIpv4Async(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;
        var addresses = await Dns.GetHostAddressesAsync(context.DnsEndPoint.Host, cancellationToken);
        foreach (var address in addresses.Where(x => x.AddressFamily == AddressFamily.InterNetwork))
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true,
            };

            try
            {
                await socket.ConnectAsync(new IPEndPoint(address, context.DnsEndPoint.Port), cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                lastException = ex;
                socket.Dispose();
            }
        }

        throw new HttpRequestException($"Unable to connect to an IPv4 address for {context.DnsEndPoint.Host}.", lastException);
    }
}
