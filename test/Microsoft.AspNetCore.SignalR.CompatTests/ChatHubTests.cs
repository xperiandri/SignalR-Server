#if NET451

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    [Collection(CompatTestsCollection.Name)]
    public class ChatHubTests
    {
        private readonly CompatTestsServerFixture _fixture;

        public static IEnumerable<ITransportFactory[]> AllTransports => TransportFactory.All;

        public ChatHubTests(CompatTestsServerFixture fixture)
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
                await client1.Broadcast("client1", "Hello, World!");

                AssertMessage("client1", "Hello, World!", await client1.WaitForMessage());
                AssertMessage("client1", "Hello, World!", await client2.WaitForMessage());
                AssertMessage("client1", "Hello, World!", await client3.WaitForMessage());
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
                await Task.WhenAll(client2.JoinGroup("test"), client4.JoinGroup("test"), client3.JoinGroup("test"));

                await client3.LeaveGroup("test");

                await client1.SendToGroup("client1", "test", "Hello, World!");

                AssertMessage("client1", "Hello, World!", await client2.WaitForMessage());
                AssertMessage("client1", "Hello, World!", await client4.WaitForMessage());
                AssertNoMessage(client1);
                AssertNoMessage(client3);
            }
        }


        [Theory(Skip="AddToGroup in OnConnected hangs randomly when using longPolling transport and JS")]
        [MemberData(nameof(AllTransports))]
        public async Task Can_broadcast_to_group_joined_on_connection(ITransportFactory transportFactory)
        {
            using (var client1 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client2 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client3 = await ChatHubTestClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
                await client1.SendToGroup("client1", "onconnectedgroup", "Hello, World!");

                AssertMessage("client1", "Hello, World!", await client1.WaitForMessage());
                AssertMessage("client1", "Hello, World!", await client2.WaitForMessage());
                AssertMessage("client1", "Hello, World!", await client3.WaitForMessage());
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
                transport.LostConnection(client1.Connection);

                await client2.Broadcast("client2", "Hello!");

                Assert.Equal(1, await reconnectTcs.Task);
                AssertMessage("client2", "Hello!", await client1.WaitForMessage());
            }

        }

        private void AssertMessage(string from, string message, ChatHubMessage msg)
        {
            Assert.Equal(from, msg.From);
            Assert.Equal(message, msg.Message);
        }

        private void AssertNoMessage(ChatHubTestClient client)
        {
            if (client.HasMessage())
            {
                var message = client.WaitForMessage().Result;
                Assert.False(true, $"Expected the client to not have received any message, but instead it has received a message from '{message.From}' containing '{message.Message}'");
            }
        }
    }
}

#endif