using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace PortForwarder.Tests;

public class TcpPortForwarderTests : IDisposable
{
    private TcpListener? _echoServer;
    private CancellationTokenSource? _echoServerCts;

    public void Dispose()
    {
        _echoServerCts?.Cancel();
        _echoServer?.Stop();
        _echoServerCts?.Dispose();
    }

    [Fact]
    public async Task ForwardsDataThroughToTarget()
    {
        var ct = TestContext.Current.CancellationToken;
        var echoPort = GetFreePort();
        var localPort = GetFreePort();

        StartEchoServer(echoPort);

        using var forwarder = new TcpPortForwarder(localPort, echoPort, "127.0.0.1",
            NullLogger<TcpPortForwarder>.Instance);
        forwarder.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, localPort, ct);
        await using var stream = client.GetStream();

        var message = "Hello, PortForwarder!"u8.ToArray();
        await stream.WriteAsync(message, ct);

        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer, ct).AsTask().WaitAsync(TimeSpan.FromSeconds(5), ct);

        var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Assert.Equal("Hello, PortForwarder!", response);
    }

    [Fact]
    public async Task HandlesMultipleClients()
    {
        var ct = TestContext.Current.CancellationToken;
        var echoPort = GetFreePort();
        var localPort = GetFreePort();

        StartEchoServer(echoPort);

        using var forwarder = new TcpPortForwarder(localPort, echoPort, "127.0.0.1",
            NullLogger<TcpPortForwarder>.Instance);
        forwarder.Start();

        var tasks = Enumerable.Range(0, 3).Select(async i =>
        {
            using var client = new TcpClient();
            await client.ConnectAsync(IPAddress.Loopback, localPort, ct);
            await using var stream = client.GetStream();

            var message = Encoding.UTF8.GetBytes($"Client {i}");
            await stream.WriteAsync(message, ct);

            var buffer = new byte[1024];
            var bytesRead = await stream.ReadAsync(buffer, ct).AsTask().WaitAsync(TimeSpan.FromSeconds(5), ct);

            return Encoding.UTF8.GetString(buffer, 0, bytesRead);
        });

        var results = await Task.WhenAll(tasks);

        Assert.Contains("Client 0", results);
        Assert.Contains("Client 1", results);
        Assert.Contains("Client 2", results);
    }

    private void StartEchoServer(int port)
    {
        _echoServerCts = new CancellationTokenSource();
        _echoServer = new TcpListener(IPAddress.Loopback, port);
        _echoServer.Start();

        var ct = _echoServerCts.Token;
        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var client = await _echoServer.AcceptTcpClientAsync(ct);
                    _ = Task.Run(async () =>
                    {
                        await using var stream = client.GetStream();
                        var buffer = new byte[65536];
                        int bytesRead;
                        while ((bytesRead = await stream.ReadAsync(buffer, ct)) > 0)
                        {
                            await stream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                        }
                    }, ct);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }, ct);
    }

    private static int GetFreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
