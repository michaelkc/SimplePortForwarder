using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PortForwarder
{
    internal class PortForwarderService : BackgroundService
    {
        private readonly ILogger<TcpPortForwarder> _forwarderLogger;
        private readonly ILogger<PortForwarderService> _logger;
        private readonly PortForwarderOptions _options;
        private readonly List<TcpPortForwarder> _forwarders = new();

        public PortForwarderService(
            IOptions<PortForwarderOptions> options,
            ILogger<PortForwarderService> logger,
            ILogger<TcpPortForwarder> forwarderLogger)
        {
            _options = options.Value;
            _logger = logger;
            _forwarderLogger = forwarderLogger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_options.Rules.Count == 0)
            {
                _logger.LogWarning("No forwarding rules configured. Add rules to the PortForwarder:Rules section in appsettings.json.");
                return;
            }

            foreach (var rule in _options.Rules)
            {
                var forwarder = new TcpPortForwarder(rule.LocalPort, rule.RemotePort, rule.RemoteHost, _forwarderLogger);
                forwarder.Start();
                _forwarders.Add(forwarder);

                _logger.LogInformation(
                    "Forwarding local port {LocalPort} to {RemoteHost}:{RemotePort}",
                    rule.LocalPort, rule.RemoteHost, rule.RemotePort);
            }

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Stopping all port forwarders");
            }
            finally
            {
                foreach (var forwarder in _forwarders)
                {
                    forwarder.Dispose();
                }
            }
        }
    }
}
