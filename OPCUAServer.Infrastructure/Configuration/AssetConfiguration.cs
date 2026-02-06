using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPCUAServer.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration model for asset signal values from appsettings.json.
    /// Supports Options Pattern.
    /// </summary>
    public class AssetConfiguration
    {
        public const string SectionName = "Assets";

        /// <summary>
        /// Dictionary of asset names to their signal configurations.
        /// Key: Asset name (e.g., "RoboticWelder_01")
        /// Value: Dictionary of signal names to values
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> Assets { get; set; } = new();
    }
}
