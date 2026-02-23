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
        private TcpPortForwarder? _forwarder;

        public PortForwarderService(
            IOptions<PortForwarderOptions> options,
            ILogger<PortForwarderService> logger,
            ILogger<TcpPortForwarder> forwarderLogger)
        {
            _options = options.Value;
            _logger = logger;
            _forwarderLogger = forwarderLogger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _forwarder = new TcpPortForwarder(_options.LocalPort, _options.RemotePort, _options.RemoteHost, _forwarderLogger);
            _forwarder.Start();

            _logger.LogInformation(
                "Forwarding local port {LocalPort} to {RemoteHost}:{RemotePort}",
                _options.LocalPort, _options.RemoteHost, _options.RemotePort);

            stoppingToken.Register(() =>
            {
                _logger.LogInformation("Stopping port forwarder");
                _forwarder.Stop();
            });

            return Task.CompletedTask;
        }
    }
}
