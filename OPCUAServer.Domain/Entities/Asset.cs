using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPCUAServer.Domain.Entities
{
    public class Asset
    {
        /// <summary>
        /// Unique identifier for the asset.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Collection of signals belonging to this asset.
        /// </summary>
        public IReadOnlyList<Signal> Signals => _signals.AsReadOnly();

        private readonly List<Signal> _signals = new();

        public Asset(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Asset name cannot be null or empty.", nameof(name));

            Name = name;
        }

        /// <summary>
        /// Adds a signal to the asset.
        /// </summary>
        public void AddSignal(Signal signal)
        {
            if (signal == null)
                throw new ArgumentNullException(nameof(signal));

            if (_signals.Any(s => s.Name == signal.Name))
                throw new InvalidOperationException($"Signal with name '{signal.Name}' already exists on asset '{Name}'.");

            _signals.Add(signal);
        }

        /// <summary>
        /// Gets a signal by name.
        /// </summary>
        public Signal? GetSignal(string signalName)
        {
            return _signals.FirstOrDefault(s => s.Name == signalName);
        }
    }
}
