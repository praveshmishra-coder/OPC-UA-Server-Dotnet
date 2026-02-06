using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OPCUAServer.Application.Interfaces;
using OPCUAServer.Application.Services;
using OPCUAServer.Infrastructure.Providers;
using OPCUAServer.Server.Hosting;
using OPCUAServer.Server.Server;

namespace OPCUAServer.Presentation;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                // ⭐ IMPORTANT FIX
                config.SetBasePath(AppContext.BaseDirectory);

                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                // ============================
                // Infrastructure Layer
                // ============================
                services.AddSingleton<ISignalProvider, AppSettingsSignalProvider>();

                // ============================
                // Application Layer
                // ============================
                services.AddSingleton<AssetService>();

                // ============================
                // OPC UA Server Layer
                // ============================
                services.AddSingleton<OpcUaServerWrapper>();

                // ============================
                // Hosted Lifecycle Service
                // ============================
                services.AddHostedService<OpcUaServerHostedService>();
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.AddConfiguration(context.Configuration.GetSection("Logging"));
            })
            .UseConsoleLifetime()
            .Build();

        await host.RunAsync();
    }
}
