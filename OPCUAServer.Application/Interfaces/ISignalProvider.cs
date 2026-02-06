using OPCUAServer.Domain.Entities;

namespace OPCUAServer.Application.Interfaces
{

    /// <summary>
    /// Abstraction for loading signal data from various sources.
    /// Follows Dependency Inversion Principle - Application layer defines the contract.
    /// </summary>
    public interface ISignalProvider
    {
        /// <summary>
        /// Retrieves asset configuration with initial signal values.
        /// </summary>
        /// <param name="assetName">Name of the asset to retrieve.</param>
        /// <returns>Configured asset with signals, or null if not found.</returns>
        Task<Asset?> GetAssetAsync(string assetName);

        /// <summary>
        /// Retrieves all configured assets.
        /// </summary>
        Task<IEnumerable<Asset>> GetAllAssetsAsync();
    }
}
