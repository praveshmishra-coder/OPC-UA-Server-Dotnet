using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OPCUAServer.Server.Server;
using OPCUAServer.Application.Services;

namespace OPCUAServer.Server.Hosting
{
    /// <summary>
    /// Background hosted service that manages the OPC UA server lifecycle.
    /// Integrates with .NET Generic Host for proper startup/shutdown.
    /// </summary>
    public class OpcUaServerHostedService : IHostedService
    {
        private readonly OpcUaServerWrapper _serverWrapper;
        private readonly AssetService _assetService;
        private readonly ILogger<OpcUaServerHostedService> _logger;

        public OpcUaServerHostedService(
            OpcUaServerWrapper serverWrapper,
            AssetService assetService,
            ILogger<OpcUaServerHostedService> logger)
        {
            _serverWrapper = serverWrapper ?? throw new ArgumentNullException(nameof(serverWrapper));
            _assetService = assetService ?? throw new ArgumentNullException(nameof(assetService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Starts the hosted service.
        /// Loads assets and starts the OPC UA server.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting OPC UA Server Hosted Service...");

                // Step 1: Load assets from configuration
                _logger.LogInformation("Loading assets...");
                await _assetService.LoadAssetsAsync();

                // Step 2: Start OPC UA server
                await _serverWrapper.StartAsync(cancellationToken);


                // Step 3: Log summary
                var assets = _assetService.GetAllAssets();
                var totalSignals = assets.Sum(a => a.Signals.Count);

                _logger.LogInformation("===================================");
                _logger.LogInformation("OPC UA Server Started Successfully");
                _logger.LogInformation("===================================");
                _logger.LogInformation("Assets Loaded: {AssetCount}", assets.Count());
                _logger.LogInformation("Signals Created: {SignalCount}", totalSignals);
                _logger.LogInformation("Server Endpoint: opc.tcp://localhost:4840");
                _logger.LogInformation("===================================");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to start OPC UA Server Hosted Service");
                throw;
            }
        }

        /// <summary>
        /// Stops the hosted service.
        /// Gracefully shuts down the OPC UA server.
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Stopping OPC UA Server Hosted Service...");
                await _serverWrapper.StopAsync(cancellationToken);
                _logger.LogInformation("OPC UA Server Hosted Service stopped successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping OPC UA Server Hosted Service");
                throw;
            }
        }
    }
}
