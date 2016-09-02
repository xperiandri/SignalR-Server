using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.IntegrationTesting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public abstract class ServerFixture : IDisposable
    {
        private readonly bool _verbose;

        private Lazy<string> _baseUrl;
        private object _lock = new object();
        private ILoggerFactory _loggerFactory;
        private IApplicationDeployer _deployer;

        public string BaseUrl => _baseUrl.Value;

        protected abstract string ServerProjectName { get; }

        public ServerFixture()
        {
            _loggerFactory = new LoggerFactory();

            _verbose = string.Equals(Environment.GetEnvironmentVariable("SIGNALR_TESTS_VERBOSE"), "1");
            if (_verbose)
            {
                _loggerFactory.AddConsole();
            }

            _baseUrl = new Lazy<string>(() => Deploy().Result);
        }

        private async Task<string> Deploy()
        {
            var url = Environment.GetEnvironmentVariable("SIGNALR_TESTS_URL");
            if (!string.IsNullOrEmpty(url))
            {
                return url;
            }

            Console.WriteLine("Deploying test server...");

            var parameters = new DeploymentParameters(
                applicationPath: GetApplicationPath(ServerProjectName),
                serverType: ServerType.Kestrel,
                runtimeFlavor: RuntimeFlavor.CoreClr,
                runtimeArchitecture: RuntimeArchitecture.x64);
            _deployer = ApplicationDeployerFactory.Create(parameters, _loggerFactory.CreateLogger("Deployment"));
            var result = _deployer.Deploy();

            // Ensure it's working
            var client = new HttpClient();
            var logger = _loggerFactory.CreateLogger("Connection");
            var resp = await RetryHelper.RetryRequest(() => client.GetAsync(result.ApplicationBaseUri), logger, result.HostShutdownToken);
            resp.EnsureSuccessStatusCode();

            Console.WriteLine("Test server ready. Running tests...");
            return result.ApplicationBaseUri;
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

            throw new InvalidOperationException($"Solution root could not be found using {applicationBasePath}");
        }

        public void Dispose()
        {
            if (_deployer != null)
            {
                _deployer.Dispose();
                _deployer = null;
            }
        }
    }
}
