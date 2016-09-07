#if NET451

using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Tests;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.FunctionalTests
{
    [Collection(FunctionalTestsCollection.Name)]
    public class HubTests
    {
        ServerFixture _serverFixture;

        public HubTests(FunctionalTestsServerFixture serverFixture)
        {
            _serverFixture = serverFixture;
        }

        [Fact]
        public async Task Can_invoke_hub_method()
        {
            using (var client1 = new HubConnection(_serverFixture.BaseUrl + "test/hubs"))
            {
                var proxy = client1.CreateHubProxy("EchoHub");
                await client1.Start();
                Assert.Equal("message", await proxy.Invoke<string>("EchoReturn", "message"));
            }
        }
    }
}

#endif