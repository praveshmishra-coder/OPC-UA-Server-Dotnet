using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OPCUAServer.Domain.Entities
{
    /// <summary>
    /// Represents a signal (data point) on an industrial asset.
    /// Supports multiple data types commonly used in industrial IoT.
    /// </summary>
    public class Signal
    {
        /// <summary>
        /// Signal identifier.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Current value of the signal.
        /// </summary>
        public object Value { get; private set; }

        /// <summary>
        /// Data type of the signal.
        /// </summary>
        public SignalDataType DataType { get; private set; }

        public Signal(string name, object value, SignalDataType dataType)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Signal name cannot be null or empty.", nameof(name));

            Name = name;
            Value = value ?? throw new ArgumentNullException(nameof(value));
            DataType = dataType;

            ValidateValueType();
        }

        /// <summary>
        /// Updates the signal value.
        /// </summary>
        public void UpdateValue(object newValue)
        {
            if (newValue == null)
                throw new ArgumentNullException(nameof(newValue));

            Value = newValue;
            ValidateValueType();
        }

        private void ValidateValueType()
        {
            var isValid = DataType switch
            {
                SignalDataType.Double => Value is double,
                SignalDataType.String => Value is string,
                SignalDataType.Integer => Value is int,
                SignalDataType.Boolean => Value is bool,
                _ => false
            };

            if (!isValid)
            {
                throw new InvalidOperationException(
                    $"Signal '{Name}' value type '{Value.GetType().Name}' does not match declared type '{DataType}'.");
            }
        }
    }

    /// <summary>
    /// Supported signal data types.
    /// </summary>
    public enum SignalDataType
    {
        Double,
        String,
        Integer,
        Boolean
    }
}
