using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace PortForwarder.Tests;

public class PortForwarderOptionsTests
{
    [Fact]
    public void BindsSingleRuleFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PortForwarder:Rules:0:LocalPort"] = "3390",
                ["PortForwarder:Rules:0:RemoteHost"] = "10.0.0.1",
                ["PortForwarder:Rules:0:RemotePort"] = "3389",
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<PortForwarderOptions>(config.GetSection("PortForwarder"));
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<PortForwarderOptions>>().Value;

        Assert.Single(options.Rules);
        Assert.Equal(3390, options.Rules[0].LocalPort);
        Assert.Equal("10.0.0.1", options.Rules[0].RemoteHost);
        Assert.Equal(3389, options.Rules[0].RemotePort);
    }

    [Fact]
    public void BindsMultipleRulesFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PortForwarder:Rules:0:LocalPort"] = "3390",
                ["PortForwarder:Rules:0:RemoteHost"] = "10.0.0.1",
                ["PortForwarder:Rules:0:RemotePort"] = "3389",
                ["PortForwarder:Rules:1:LocalPort"] = "8080",
                ["PortForwarder:Rules:1:RemoteHost"] = "10.0.0.2",
                ["PortForwarder:Rules:1:RemotePort"] = "80",
            })
            .Build();

        var services = new ServiceCollection();
        services.Configure<PortForwarderOptions>(config.GetSection("PortForwarder"));
        var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<PortForwarderOptions>>().Value;

        Assert.Equal(2, options.Rules.Count);
        Assert.Equal(3390, options.Rules[0].LocalPort);
        Assert.Equal(8080, options.Rules[1].LocalPort);
        Assert.Equal("10.0.0.2", options.Rules[1].RemoteHost);
        Assert.Equal(80, options.Rules[1].RemotePort);
    }

    [Fact]
    public void DefaultsToEmptyRules()
    {
        var options = new PortForwarderOptions();

        Assert.Empty(options.Rules);
    }
}
