using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OPCUAServer.Application.Services;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OPCUAServer.Server.Services
{
    /// <summary>
    /// Polling interval implement करने के लिए - periodically signal values update करता है
    /// </summary>
    public class SignalUpdateService : BackgroundService
    {
        private readonly AssetService _assetService;
        private readonly ILogger<SignalUpdateService> _logger;
        private readonly TimeSpan _pollingInterval;
        private readonly Random _random = new();

        public SignalUpdateService(
            AssetService assetService,
            ILogger<SignalUpdateService> logger)
        {
            _assetService = assetService;
            _logger = logger;
            _pollingInterval = TimeSpan.FromSeconds(5); // 5 second polling interval
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Signal Update Service started with polling interval: {Interval}", _pollingInterval);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await UpdateAllSignalsAsync();
                    await Task.Delay(_pollingInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating signals");
                }
            }

            _logger.LogInformation("Signal Update Service stopped");
        }

        private async Task UpdateAllSignalsAsync()
        {
            var assets = _assetService.GetAllAssets();

            foreach (var asset in assets)
            {
                foreach (var signal in asset.Signals)
                {
                    // Simulate value changes based on data type
                    object newValue = signal.DataType switch
                    {
                        Domain.Entities.SignalDataType.Double => Math.Round(50 + _random.NextDouble() * 50, 2),
                        Domain.Entities.SignalDataType.Integer => _random.Next(0, 100),
                        Domain.Entities.SignalDataType.Boolean => _random.Next(0, 2) == 1,
                        Domain.Entities.SignalDataType.String => $"Status_{DateTime.Now:HHmmss}",
                        _ => signal.Value
                    };

                    signal.UpdateValue(newValue);

                    _logger.LogDebug("Updated: {Asset}.{Signal} = {Value}",
                        asset.Name, signal.Name, newValue);
                }
            }

            await Task.CompletedTask;
        }
    }
}