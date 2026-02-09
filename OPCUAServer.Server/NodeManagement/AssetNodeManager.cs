using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Server;
using OPCUAServer.Application.Services;
using OPCUAServer.Domain.Entities;

namespace OPCUAServer.Server.NodeManagement
{
    public class AssetNodeManager : CustomNodeManager2
    {
        private readonly AssetService _assetService;
        private readonly ILogger<AssetNodeManager> _logger;
        private FolderState _assetsFolder;  // ⚠️ Warning fix: removed '?'

        // 👇 NEW: Signal nodes को track करने के लिए
        private readonly Dictionary<string, BaseDataVariableState> _signalNodes = new();
        private Timer? _updateTimer;

        public AssetNodeManager(
            IServerInternal server,
            ApplicationConfiguration configuration,
            AssetService assetService,
            ILogger<AssetNodeManager> logger)
            : base(server, configuration, "http://opcuacleanserver.com/assets")
        {
            _assetService = assetService;
            _logger = logger;
            SystemContext.NodeIdFactory = this;
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                base.CreateAddressSpace(externalReferences);

                _logger.LogInformation("Creating OPC UA Address Space...");

                // ===== CREATE ROOT ASSETS FOLDER =====
                _assetsFolder = new FolderState(null)
                {
                    NodeId = new NodeId("Assets", NamespaceIndex),
                    BrowseName = new QualifiedName("Assets", NamespaceIndex),
                    DisplayName = new LocalizedText("Assets"),
                    TypeDefinitionId = ObjectTypeIds.FolderType
                };

                // Link to Objects Folder
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                _assetsFolder.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
                references.Add(new NodeStateReference(
                    ReferenceTypeIds.Organizes,
                    false,
                    _assetsFolder.NodeId));

                AddPredefinedNode(SystemContext, _assetsFolder);

                // ===== LOAD ASSETS =====
                var assets = _assetService.GetAllAssets();

                foreach (var asset in assets)
                {
                    CreateAssetNodes(asset);
                }

                _logger.LogInformation("OPC UA Address Space Created Successfully");

                // 👇 NEW: Start update timer for pub-sub functionality
                StartValueUpdateTimer();
            }
        }

        private void CreateAssetNodes(Asset asset)
        {
            var assetFolder = new FolderState(_assetsFolder)
            {
                NodeId = new NodeId(asset.Name, NamespaceIndex),
                BrowseName = new QualifiedName(asset.Name, NamespaceIndex),
                DisplayName = new LocalizedText(asset.Name),
                TypeDefinitionId = ObjectTypeIds.FolderType
            };

            _assetsFolder.AddChild(assetFolder);
            AddPredefinedNode(SystemContext, assetFolder);

            foreach (var signal in asset.Signals)
            {
                CreateSignalVariable(assetFolder, asset.Name, signal);
            }

            _logger.LogInformation(
                "Created Asset Node {Asset} with {Count} signals",
                asset.Name,
                asset.Signals.Count);
        }

        private void CreateSignalVariable(
            FolderState parent,
            string assetName,
            Signal signal)
        {
            var variable = new BaseDataVariableState(parent)
            {
                NodeId = new NodeId($"{assetName}.{signal.Name}", NamespaceIndex),
                BrowseName = new QualifiedName(signal.Name, NamespaceIndex),
                DisplayName = new LocalizedText(signal.Name),
                TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
                DataType = MapToOpcDataType(signal.DataType),  // ✅ यह method नीचे define है
                ValueRank = ValueRanks.Scalar,
                AccessLevel = AccessLevels.CurrentReadOrWrite,
                UserAccessLevel = AccessLevels.CurrentReadOrWrite,
                Historizing = false,
                Value = signal.Value,
                StatusCode = StatusCodes.Good,
                Timestamp = DateTime.UtcNow
            };

            parent.AddChild(variable);
            AddPredefinedNode(SystemContext, variable);

            // 👇 NEW: Store reference for updates
            string key = $"{assetName}.{signal.Name}";
            _signalNodes[key] = variable;

            _logger.LogDebug(
                "Signal Node Created: {Asset}.{Signal}",
                assetName,
                signal.Name);
        }

        // ✅ यह method missing था
        private NodeId MapToOpcDataType(SignalDataType dataType)
        {
            return dataType switch
            {
                SignalDataType.Double => DataTypeIds.Double,
                SignalDataType.String => DataTypeIds.String,
                SignalDataType.Integer => DataTypeIds.Int32,
                SignalDataType.Boolean => DataTypeIds.Boolean,
                _ => DataTypeIds.BaseDataType
            };
        }

        // 👇 NEW: Pub-Sub implementation - periodically update OPC UA nodes
        private void StartValueUpdateTimer()
        {
            _updateTimer = new Timer(UpdateNodeValues, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            _logger.LogInformation("Started node value update timer (1 second interval)");
        }

        private void UpdateNodeValues(object? state)
        {
            lock (Lock)
            {
                try
                {
                    var assets = _assetService.GetAllAssets();

                    foreach (var asset in assets)
                    {
                        foreach (var signal in asset.Signals)
                        {
                            string key = $"{asset.Name}.{signal.Name}";

                            if (_signalNodes.TryGetValue(key, out var variable))
                            {
                                // Update OPC UA node with current signal value
                                variable.Value = signal.Value;
                                variable.Timestamp = DateTime.UtcNow;
                                variable.StatusCode = StatusCodes.Good;
                                variable.ClearChangeMasks(SystemContext, false);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating node values");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _updateTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}