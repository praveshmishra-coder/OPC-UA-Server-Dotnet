using OPCUAServer.Application.Interfaces;
using OPCUAServer.Domain.Entities;
using Microsoft.Extensions.Logging;



namespace OPCUAServer.Application.Services
{
    /// <summary>
    /// Application service for managing assets and their signals.
    /// Orchestrates business logic without infrastructure concerns.
    /// </summary>
    public class AssetService
    {
        private readonly ISignalProvider _signalProvider;
        private readonly ILogger<AssetService> _logger;
        private readonly Dictionary<string, Asset> _assetCache = new();

        public AssetService(
            ISignalProvider signalProvider,
            ILogger<AssetService> logger)
        {
            _signalProvider = signalProvider ?? throw new ArgumentNullException(nameof(signalProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Loads and caches all assets from the configured provider.
        /// </summary>
        public async Task<IEnumerable<Asset>> LoadAssetsAsync()
        {
            _logger.LogInformation("Loading assets from signal provider...");

            var assets = await _signalProvider.GetAllAssetsAsync();

            foreach (var asset in assets)
            {
                _assetCache[asset.Name] = asset;
                _logger.LogInformation(
                    "Asset Loaded: {AssetName} with {SignalCount} signals",
                    asset.Name,
                    asset.Signals.Count);

                foreach (var signal in asset.Signals)
                {
                    _logger.LogDebug(
                        "Signal Created: {AssetName}.{SignalName} = {Value} ({DataType})",
                        asset.Name,
                        signal.Name,
                        signal.Value,
                        signal.DataType);
                }
            }

            return assets;
        }

        /// <summary>
        /// Retrieves a cached asset by name.
        /// </summary>
        public Asset? GetAsset(string assetName)
        {
            _assetCache.TryGetValue(assetName, out var asset);
            return asset;
        }

        /// <summary>
        /// Retrieves all cached assets.
        /// </summary>
        public IEnumerable<Asset> GetAllAssets()
        {
            return _assetCache.Values;
        }
    }
}
