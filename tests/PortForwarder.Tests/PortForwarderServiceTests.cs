using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace PortForwarder.Tests;

public class PortForwarderServiceTests
{
    [Fact]
    public async Task StartsAndStopsCleanly()
    {
        var echoPort = GetFreePort();
        var localPort = GetFreePort();

        StartEchoServer(echoPort, out var cts);

        var options = Options.Create(new PortForwarderOptions
        {
            LocalPort = localPort,
            RemoteHost = "127.0.0.1",
            RemotePort = echoPort,
        });

        var service = new PortForwarderService(
            options,
            NullLogger<PortForwarderService>.Instance,
            NullLogger<TcpPortForwarder>.Instance);

        using var serviceCts = new CancellationTokenSource();
        await service.StartAsync(serviceCts.Token);

        // Verify the service is listening by connecting
        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, localPort);
        client.Close();

        serviceCts.Cancel();
        await service.StopAsync(CancellationToken.None);

        cts.Cancel();
        cts.Dispose();
    }

    private static void StartEchoServer(int port, out CancellationTokenSource cts)
    {
        cts = new CancellationTokenSource();
        var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        var ct = cts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var client = await listener.AcceptTcpClientAsync(ct);
                    client.Close();
                }
            }
            catch (OperationCanceledException) { }
            finally { listener.Stop(); }
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
