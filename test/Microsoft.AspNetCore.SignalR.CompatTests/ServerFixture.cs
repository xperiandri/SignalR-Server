using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    public class ServerFixture : IDisposable
    {
        private readonly bool _verbose;

        private Lazy<ServerInfo> _info;
        private object _lock = new object();
        private ILoggerFactory _loggerFactory;
        private IApplicationDeployer _deployer;

        public ServerInfo ServerInfo => _info.Value;

        public ServerFixture()
        {
            _loggerFactory = new LoggerFactory();

            _verbose = string.Equals(Environment.GetEnvironmentVariable("SIGNALR_COMPAT_TESTS_VERBOSE"), "1");

            _info = new Lazy<ServerInfo>(() => Deploy().Result);
        }

        private async Task<ServerInfo> Deploy()
        {
            var url = Environment.GetEnvironmentVariable("SIGNALR_COMPAT_TESTS_URL");
            if(!string.IsNullOrEmpty(url))
            {
                return new ServerInfo(url);
            }

            var parameters = new DeploymentParameters(
                applicationPath: GetApplicationPath("Microsoft.AspNetCore.SignalR.CompatTests.Server"),
                serverType: ServerType.Kestrel,
                runtimeFlavor: RuntimeFlavor.CoreClr,
                runtimeArchitecture: RuntimeArchitecture.x64);
            _deployer = ApplicationDeployerFactory.Create(parameters, _loggerFactory.CreateLogger("Deployment"));
            var result = _deployer.Deploy();

            // Ensure it's working
            var client = new HttpClient();
            var logger = _loggerFactory.CreateLogger("Connection");
            var resp = await RetryHelper.RetryRequest(() => client.GetAsync(result.ApplicationBaseUri), logger);
            resp.EnsureSuccessStatusCode();

            return new ServerInfo(result.ApplicationBaseUri);
        }

        private static string GetApplicationPath(string projectName)
        {
            var applicationBasePath = PlatformServices.Default.Application.ApplicationBasePath;

            var directoryInfo = new DirectoryInfo(applicationBasePath);
            do
            {
                var solutionFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, "SignalR-Server.sln"));
                if (solutionFileInfo.Exists)
                {
                    return Path.GetFullPath(Path.Combine(directoryInfo.FullName, "test", projectName));
                }

                directoryInfo = directoryInfo.Parent;
            }
            while (directoryInfo.Parent != null);

            throw new Exception($"Solution root could not be found using {applicationBasePath}");
        }

        public void Dispose()
        {
            if(_deployer != null)
            {
                _deployer.Dispose();
            }
        }
    }
}
