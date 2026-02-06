using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using OPCUAServer.Application.Services;
using OPCUAServer.Server.Core;

namespace OPCUAServer.Server.Server
{
    public class OpcUaServerWrapper : IDisposable
    {
        private readonly AssetService _assetService;
        private readonly ILogger<OpcUaServerWrapper> _logger;
        private readonly ILoggerFactory _loggerFactory;

        private StandardServer? _server;
        private ApplicationConfiguration? _configuration;

        public OpcUaServerWrapper(
            AssetService assetService,
            ILogger<OpcUaServerWrapper> logger,
            ILoggerFactory loggerFactory)
        {
            _assetService = assetService;
            _logger = logger;
            _loggerFactory = loggerFactory;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting OPC UA Server...");

            _configuration = BuildApplicationConfiguration();

            await _configuration.ValidateAsync(ApplicationType.Server);

            // ✅ CERTIFICATE AUTO CREATE
            var application = new ApplicationInstance
            {
                ApplicationName = _configuration.ApplicationName,
                ApplicationType = ApplicationType.Server,
                ApplicationConfiguration = _configuration
            };

            bool certOK = await application.CheckApplicationInstanceCertificatesAsync(false, 2048);

            if (!certOK)
                throw new Exception("Application instance certificate invalid!");

            // ✅ create server
            _server = new MyOpcUaServer(_assetService, _loggerFactory);

            _server.Start(_configuration);

            _logger.LogInformation("Server Started at opc.tcp://localhost:4840");
        }



        public Task StopAsync(CancellationToken cancellationToken)
        {
            _server?.Stop();
            _logger.LogInformation("Server Stopped");
            return Task.CompletedTask;
        }





        private ApplicationConfiguration BuildApplicationConfiguration()
        {
            var basePath = AppContext.BaseDirectory;
            var pkiRoot = Path.Combine(basePath, "pki");

            // ✅ create PKI folders
            Directory.CreateDirectory(Path.Combine(pkiRoot, "own"));
            Directory.CreateDirectory(Path.Combine(pkiRoot, "trusted"));
            Directory.CreateDirectory(Path.Combine(pkiRoot, "issuer"));
            Directory.CreateDirectory(Path.Combine(pkiRoot, "rejected"));

            return new ApplicationConfiguration
            {
                ApplicationName = "OpcUaCleanServer",
                ApplicationType = ApplicationType.Server,
                ApplicationUri = "urn:localhost:OpcUaCleanServer",

                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiRoot, "own"),
                        SubjectName = "CN=OpcUaCleanServer"
                    },

                    TrustedPeerCertificates = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiRoot, "trusted")
                    },

                    TrustedIssuerCertificates = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiRoot, "issuer")
                    },

                    RejectedCertificateStore = new CertificateTrustList
                    {
                        StoreType = CertificateStoreType.Directory,
                        StorePath = Path.Combine(pkiRoot, "rejected")
                    },

                    AutoAcceptUntrustedCertificates = true
                },

                ServerConfiguration = new ServerConfiguration
                {
                    BaseAddresses = new StringCollection
            {
                "opc.tcp://localhost:4840"
            }
                },

                TransportConfigurations = new TransportConfigurationCollection(),

                TransportQuotas = new TransportQuotas
                {
                    OperationTimeout = 15000
                },

                ClientConfiguration = new ClientConfiguration()
            };
        }



        public void Dispose()
        {
            _server?.Dispose();
        }
    }
}
