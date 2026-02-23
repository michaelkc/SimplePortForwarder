using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace PortForwarder
{
    internal sealed class TcpPortForwarder : IDisposable
    {
        private const int MaxConnectRetries = 3;
        private const int BufferSize = 65536;

        private readonly int _localPort;
        private readonly ILogger<TcpPortForwarder> _logger;
        private readonly string _targetHost;
        private readonly int _targetPort;
        private readonly CancellationTokenSource _cts = new();
        private readonly List<Task> _activeConnections = new();
        private TcpListener? _listener;
        private Task? _acceptLoop;

        public TcpPortForwarder(int localPort, int targetPort, string targetHost, ILogger<TcpPortForwarder> logger)
        {
            _localPort = localPort;
            _targetPort = targetPort;
            _targetHost = targetHost;
            _logger = logger;
        }

        public void Start()
        {
            _listener = new TcpListener(IPAddress.Any, _localPort);
            _listener.Start();
            _acceptLoop = AcceptLoopAsync(_cts.Token);
        }

        public void Stop()
        {
            _cts.Cancel();
            _listener?.Stop();

            try
            {
                Task.WhenAll(_activeConnections).GetAwaiter().GetResult();
            }
            catch
            {
                // Expected during shutdown
            }
        }

        public void Dispose()
        {
            Stop();
            _cts.Dispose();
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var sourceClient = await _listener!.AcceptTcpClientAsync(ct);
                    var connectionTask = HandleClientAsync(sourceClient, ct);
                    _activeConnections.Add(connectionTask);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed when trying to accept new clients");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient sourceClient, CancellationToken ct)
        {
            using var source = sourceClient;

            TcpClient? targetClient = null;
            for (var attempt = 0; attempt < MaxConnectRetries; attempt++)
            {
                try
                {
                    targetClient = new TcpClient();
                    await targetClient.ConnectAsync(_targetHost, _targetPort, ct);
                    break;
                }
                catch (OperationCanceledException)
                {
                    targetClient?.Dispose();
                    return;
                }
                catch (SocketException) when (attempt < MaxConnectRetries - 1)
                {
                    _logger.LogWarning("Retrying connect (attempt {Attempt})", attempt + 1);
                    targetClient?.Dispose();
                    targetClient = null;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Connection failed");
                    targetClient?.Dispose();
                    return;
                }
            }

            if (targetClient is null)
                return;

            using var target = targetClient;

            try
            {
                await using var sourceStream = source.GetStream();
                await using var targetStream = target.GetStream();

                var sourceToTarget = CopyStreamAsync(sourceStream, targetStream, "client", ct);
                var targetToSource = CopyStreamAsync(targetStream, sourceStream, "server", ct);

                await Task.WhenAny(sourceToTarget, targetToSource);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Connection closed: '{Message}'", ex.Message);
            }
        }

        private async Task CopyStreamAsync(NetworkStream from, NetworkStream to, string direction, CancellationToken ct)
        {
            var buffer = new byte[BufferSize];
            try
            {
                int bytesRead;
                while ((bytesRead = await from.ReadAsync(buffer, ct)) > 0)
                {
                    await to.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogInformation("{Direction} disconnected: '{Message}'", direction, ex.Message);
            }
        }
    }
}
