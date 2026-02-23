using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PortForwarder;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddWindowsService();

builder.Services.Configure<PortForwarderOptions>(
    builder.Configuration.GetSection("PortForwarder"));

builder.Services.AddHostedService<PortForwarderService>();

var host = builder.Build();
host.Run();
