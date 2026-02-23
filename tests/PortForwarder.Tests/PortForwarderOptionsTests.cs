using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace PortForwarder.Tests;

public class PortForwarderOptionsTests
{
    [Fact]
    public void BindsFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PortForwarder:LocalPort"] = "3390",
                ["PortForwarder:RemoteHost"] = "10.0.0.1",
                ["PortForwarder:RemotePort"] = "3389",
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<PortForwarderOptions>(config.GetSection("PortForwarder"));
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<PortForwarderOptions>>().Value;

        Assert.Equal(3390, options.LocalPort);
        Assert.Equal("10.0.0.1", options.RemoteHost);
        Assert.Equal(3389, options.RemotePort);
    }

    [Fact]
    public void DefaultsToEmptyValues()
    {
        var options = new PortForwarderOptions();

        Assert.Equal(0, options.LocalPort);
        Assert.Equal(string.Empty, options.RemoteHost);
        Assert.Equal(0, options.RemotePort);
    }
}
