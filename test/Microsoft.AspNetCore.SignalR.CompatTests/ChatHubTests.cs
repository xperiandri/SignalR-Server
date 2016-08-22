using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    public class ChatHubTests : IClassFixture<ServerFixture>
    {
        private readonly ServerFixture _fixture;

        public static IEnumerable<object[]> Transports
        {
            get
            {
                yield return new [] { new TransportFactory<WebSocketTransport>() };
                yield return new[] { new TransportFactory<LongPollingTransport>() };
                yield return new [] { new TransportFactory<ServerSentEventsTransport>() };
            }
        }

        public ChatHubTests(ServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [MemberData(nameof(Transports))]
        public async Task Can_Connect_To_Server(ITransportFactory transportFactory)
        {
            using (var client = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            {
            }
        }

        [Theory]
        [MemberData(nameof(Transports))]
        public async Task Can_Handle_RPC(ITransportFactory transportFactory)
        {
            using (var client = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            {
                Assert.Equal(42, await client.Add(40, 2));
                Assert.Equal(42, await client.AddAsync(40, 2));
            }
        }

        [Theory]
        [MemberData(nameof(Transports))]
        public async Task Can_Broadcast_To_Clients(ITransportFactory transportFactory)
        {
            using (var client1 = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            using (var client2 = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            using (var client3 = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            {
                await Task.WhenAll(client1.SetName("client1"), client2.SetName("client2"), client3.SetName("client3"));

                await client1.Broadcast("Hello, World!");

                AssertMessage("client1", "Hello, World!", await client1.WaitForMessage());
                AssertMessage("client1", "Hello, World!", await client2.WaitForMessage());
                AssertMessage("client1", "Hello, World!", await client3.WaitForMessage());
            }
        }

        [Theory]
        [MemberData(nameof(Transports))]
        public async Task ClientState_Can_Be_Read_By_Client(ITransportFactory transportFactory)
        {
            using (var client1 = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            {
                Assert.Null(client1.Proxy.GetValue<string>("Username"));

                await client1.SetName("client1");

                Assert.Equal("client1", client1.Proxy.GetValue<string>("Username"));
            }
        }

        [Theory]
        [MemberData(nameof(Transports))]
        public async Task Can_Broadcast_To_Groups(ITransportFactory transportFactory)
        {
            using (var client1 = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            using (var client2 = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            using (var client3 = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            using (var client4 = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            {
                await Task.WhenAll(client1.SetName("client1"), client2.SetName("client2"), client3.SetName("client3"), client4.SetName("client4"));

                await Task.WhenAll(client2.JoinGroup("test"), client4.JoinGroup("test"), client3.JoinGroup("test"));

                await client3.LeaveGroup("test");

                await client1.SendToGroup("test", "Hello, World!");

                AssertMessage("client1", "Hello, World!", await client2.WaitForMessage());
                AssertMessage("client1", "Hello, World!", await client4.WaitForMessage());
                Assert.False(client1.HasMessage());
                Assert.False(client3.HasMessage());
            }
        }

        [Theory]
        [MemberData(nameof(Transports))]
        public async Task Can_Broadcast_To_Group_Joined_On_Connection(ITransportFactory transportFactory)
        {
            using (var client1 = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            using (var client2 = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            using (var client3 = await ChatHubTestClient.Connect(_fixture.ServerInfo.HubConnectionUrl, transportFactory.Create()))
            {
                await client1.SendToGroup("onconnectedgroup", "Hello, World!");

                AssertMessage("unknown", "Hello, World!", await client1.WaitForMessage());
                AssertMessage("unknown", "Hello, World!", await client2.WaitForMessage());
                AssertMessage("unknown", "Hello, World!", await client3.WaitForMessage());
            }
        }

        private void AssertMessage(string from, string message, ChatHubMessage msg)
        {
            Assert.Equal(from, msg.From);
            Assert.Equal(message, msg.Message);
        }
    }
}
