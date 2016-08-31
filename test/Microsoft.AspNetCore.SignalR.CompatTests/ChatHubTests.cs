using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    [Collection(ServerTestsCollection.Name)]
    public class ChatHubTests
    {
        private readonly ServerFixture _fixture;

        public static IEnumerable<ITransportFactory[]> AllTransports => TransportFactory.All;

        public ChatHubTests(ServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [MemberData(nameof(AllTransports))]
        public async Task Can_connect_to_server(ITransportFactory transportFactory)
        {
            using (var client = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
            }
        }

        [Theory]
        [MemberData(nameof(AllTransports))]
        public async Task Can_handle_RPC(ITransportFactory transportFactory)
        {
            using (var client = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
                Assert.Equal(42, await client.Add(40, 2));
                Assert.Equal(42, await client.AddAsync(40, 2));
            }
        }

        [Theory]
        [MemberData(nameof(AllTransports))]
        public async Task Can_broadcast_to_clients(ITransportFactory transportFactory)
        {
            using (var client1 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client2 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client3 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
                await Task.WhenAll(client1.SetName("client1"), client2.SetName("client2"), client3.SetName("client3"));

                await client1.Broadcast("Hello, World!");

                AssertMessage("client1", "Hello, World!", await client1.WaitForMessage());
                AssertMessage("client1", "Hello, World!", await client2.WaitForMessage());
                AssertMessage("client1", "Hello, World!", await client3.WaitForMessage());
            }
        }

        [Theory]
        [MemberData(nameof(AllTransports))]
        public async Task ClientState_can_be_read_by_client(ITransportFactory transportFactory)
        {
            // NOTE: ClientState is probably going to be removed. However it is important to test how the managed client behaves when trying
            // to access it. When ClientState is removed, this test will likely fail and should be adjusted (not removed) to specify the expected
            // behavior (i.e. returning null or failing to read values, or whatever)

            using (var client1 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
                Assert.Null(client1.Proxy.GetValue<string>("Username"));

                await client1.SetName("client1");
                Assert.Equal("client1", client1.Proxy.GetValue<string>("Username"));

                client1.Proxy["Username"] = "client1prime";

                await client1.Broadcast("Hello, World!");

                Assert.Equal("client1prime", client1.Proxy["Username"]);
                AssertMessage("client1prime", "Hello, World!", await client1.WaitForMessage());
            }
        }

        [Theory]
        [MemberData(nameof(AllTransports))]
        public async Task Can_broadcast_to_groups(ITransportFactory transportFactory)
        {
            using (var client1 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client2 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client3 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client4 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
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
        [MemberData(nameof(AllTransports))]
        public async Task Can_broadcast_to_group_joined_on_connection(ITransportFactory transportFactory)
        {
            using (var client1 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client2 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client3 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
                await client1.SendToGroup("onconnectedgroup", "Hello, World!");

                AssertMessage("unknown", "Hello, World!", await client1.WaitForMessage());
                AssertMessage("unknown", "Hello, World!", await client2.WaitForMessage());
                AssertMessage("unknown", "Hello, World!", await client3.WaitForMessage());
            }
        }

        [Theory]
        [MemberData(nameof(AllTransports))]
        public async Task Can_receive_progress_messages(ITransportFactory transportFactory)
        {

            using (var client1 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
                await client1.WithProgress();

                Assert.Equal(new[] { 0, 1, 2, 3, 4 }, client1.ReceivedProgress.ToArray());
            }
        }

        [Theory]
        [MemberData(nameof(AllTransports))]
        public async Task Can_recover_from_reconnect(ITransportFactory transportFactory)
        {
            var reconnectTcs = new TaskCompletionSource<int>();
            var transport = transportFactory.Create();
            using (var client1 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transport))
            using (var client2 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
                client1.Connection.Reconnected += () => reconnectTcs.SetResult(1);

                await client1.SetName("client1");

                transport.LostConnection(client1.Connection);

                await client2.SetName("client2");

                await client2.Broadcast("Hello!");

                Assert.Equal(1, await reconnectTcs.Task);
                AssertMessage("client2", "Hello!", await client1.WaitForMessage());
            }

        }

        private void AssertMessage(string from, string message, ChatHubMessage msg)
        {
            Assert.Equal(from, msg.From);
            Assert.Equal(message, msg.Message);
        }
    }
}
