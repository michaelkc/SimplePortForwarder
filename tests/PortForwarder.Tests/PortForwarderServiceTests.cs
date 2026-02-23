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
            Rules = [new ForwardingRule
            {
                LocalPort = localPort,
                RemoteHost = "127.0.0.1",
                RemotePort = echoPort,
            }]
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

    [Fact]
    public async Task StartsMultipleForwarders()
    {
        var echoPort1 = GetFreePort();
        var echoPort2 = GetFreePort();
        var localPort1 = GetFreePort();
        var localPort2 = GetFreePort();

        StartEchoServer(echoPort1, out var cts1);
        StartEchoServer(echoPort2, out var cts2);

        var options = Options.Create(new PortForwarderOptions
        {
            Rules =
            [
                new ForwardingRule { LocalPort = localPort1, RemoteHost = "127.0.0.1", RemotePort = echoPort1 },
                new ForwardingRule { LocalPort = localPort2, RemoteHost = "127.0.0.1", RemotePort = echoPort2 },
            ]
        });

        var service = new PortForwarderService(
            options,
            NullLogger<PortForwarderService>.Instance,
            NullLogger<TcpPortForwarder>.Instance);

        using var serviceCts = new CancellationTokenSource();
        await service.StartAsync(serviceCts.Token);

        // Verify both forwarders are listening
        using var client1 = new TcpClient();
        await client1.ConnectAsync(IPAddress.Loopback, localPort1);
        client1.Close();

        using var client2 = new TcpClient();
        await client2.ConnectAsync(IPAddress.Loopback, localPort2);
        client2.Close();

        serviceCts.Cancel();
        await service.StopAsync(CancellationToken.None);

        cts1.Cancel(); cts1.Dispose();
        cts2.Cancel(); cts2.Dispose();
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
