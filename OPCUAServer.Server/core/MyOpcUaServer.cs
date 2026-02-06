using Opc.Ua;
using Opc.Ua.Server;
using OPCUAServer.Application.Services;
using OPCUAServer.Server.NodeManagement;
using Microsoft.Extensions.Logging;

namespace OPCUAServer.Server.Core
{
    public class MyOpcUaServer : StandardServer
    {
        private readonly AssetService _assetService;
        private readonly ILoggerFactory _loggerFactory;

        public MyOpcUaServer(
            AssetService assetService,
            ILoggerFactory loggerFactory)
        {
            _assetService = assetService;
            _loggerFactory = loggerFactory;
        }

        protected override MasterNodeManager CreateMasterNodeManager(
            IServerInternal server,
            ApplicationConfiguration configuration)
        {
            return new MasterNodeManager(
                server,
                configuration,
                null,
                new AssetNodeManager(
                    server,
                    configuration,
                    _assetService,
                    _loggerFactory.CreateLogger<AssetNodeManager>()
                )
            );
        }
    }
}
