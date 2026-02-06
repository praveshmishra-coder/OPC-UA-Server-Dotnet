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
        private FolderState? _assetsFolder;

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

            _assetsFolder?.AddChild(assetFolder);
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
                DataType = MapToOpcDataType(signal.DataType),
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

            _logger.LogDebug(
                "Signal Node Created: {Asset}.{Signal}",
                assetName,
                signal.Name);
        }

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
    }
}
