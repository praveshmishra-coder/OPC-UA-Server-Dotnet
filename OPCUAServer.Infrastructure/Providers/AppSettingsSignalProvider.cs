using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OPCUAServer.Application.Interfaces;
using OPCUAServer.Domain.Entities;
using System.Text.Json;

namespace OPCUAServer.Infrastructure.Providers
{

    /// <summary>
    /// Implementation of ISignalProvider that loads asset data from appsettings.json.
    /// Infrastructure concern - depends on Microsoft.Extensions.Configuration.
    /// </summary>
    public class AppSettingsSignalProvider : ISignalProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AppSettingsSignalProvider> _logger;

        public AppSettingsSignalProvider(
            IConfiguration configuration,
            ILogger<AppSettingsSignalProvider> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<Asset?> GetAssetAsync(string assetName)
        {
            _logger.LogDebug("Loading asset configuration for: {AssetName}", assetName);

            var assetSection = _configuration.GetSection($"Assets:{assetName}");

            if (!assetSection.Exists())
            {
                _logger.LogWarning("Asset configuration not found: {AssetName}", assetName);
                return Task.FromResult<Asset?>(null);
            }

            var asset = new Asset(assetName);

            foreach (var signalConfig in assetSection.GetChildren())
            {
                var signalName = signalConfig.Key;
                var signalValue = signalConfig.Value;

                if (string.IsNullOrEmpty(signalValue))
                {
                    _logger.LogWarning("Skipping signal with null value: {AssetName}.{SignalName}", assetName, signalName);
                    continue;
                }

                var (value, dataType) = ParseSignalValue(signalValue);
                var signal = new Signal(signalName, value, dataType);
                asset.AddSignal(signal);

                _logger.LogDebug(
                    "Loaded signal: {AssetName}.{SignalName} = {Value} ({DataType})",
                    assetName,
                    signalName,
                    value,
                    dataType);
            }

            return Task.FromResult<Asset?>(asset);
        }

        public async Task<IEnumerable<Asset>> GetAllAssetsAsync()
        {
            _logger.LogInformation("Loading all asset configurations from appsettings.json");

            var assetsSection = _configuration.GetSection("Assets");

            if (!assetsSection.Exists())
            {
                _logger.LogWarning("No Assets section found in configuration");
                return Enumerable.Empty<Asset>();
            }

            var assets = new List<Asset>();

            foreach (var assetSection in assetsSection.GetChildren())
            {
                var assetName = assetSection.Key;
                var asset = await GetAssetAsync(assetName);

                if (asset != null)
                {
                    assets.Add(asset);
                }
            }

            _logger.LogInformation("Loaded {AssetCount} assets from configuration", assets.Count);

            return assets;
        }

        /// <summary>
        /// Parses signal value from string and determines its data type.
        /// Supports: double, int, bool, string.
        /// </summary>
        private (object Value, SignalDataType DataType) ParseSignalValue(string valueString)
        {
            // Try parsing as double
            if (double.TryParse(valueString, out var doubleValue))
            {
                return (doubleValue, SignalDataType.Double);
            }

            // Try parsing as integer
            if (int.TryParse(valueString, out var intValue))
            {
                return (intValue, SignalDataType.Integer);
            }

            // Try parsing as boolean
            if (bool.TryParse(valueString, out var boolValue))
            {
                return (boolValue, SignalDataType.Boolean);
            }

            // Default to string
            return (valueString, SignalDataType.String);
        }
    }
}
