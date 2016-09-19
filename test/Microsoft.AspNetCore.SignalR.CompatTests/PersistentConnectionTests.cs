#if NET451

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    [Collection(CompatTestsCollection.Name)]
    public class PersistentConnectionTests
    {
        private readonly CompatTestsServerFixture _fixture;

        public static IEnumerable<ITransportFactory[]> AllTransports => TransportFactory.All;

        public PersistentConnectionTests(CompatTestsServerFixture fixture)
        {
            _fixture = fixture;
        }

        [Theory]
        [MemberData(nameof(AllTransports))]
        public async Task Can_connect_to_server(ITransportFactory transportFactory)
        {
            using (var client = await TestConnectionClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
            }
        }

        [Theory]
        [MemberData(nameof(AllTransports))]
        public async Task Can_broadcast_to_clients(ITransportFactory transportFactory)
        {
            using (var client1 = await TestConnectionClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client2 = await TestConnectionClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client3 = await TestConnectionClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
                await client1.Broadcast("Hello, World!");

                AssertMessage(client1.Connection.ConnectionId, "Hello, World!", await client1.WaitForMessage());
                AssertMessage(client1.Connection.ConnectionId, "Hello, World!", await client2.WaitForMessage());
                AssertMessage(client1.Connection.ConnectionId, "Hello, World!", await client3.WaitForMessage());
            }
        }

        [Theory]
        [MemberData(nameof(AllTransports))]
        public async Task Can_broadcast_to_groups(ITransportFactory transportFactory)
        {
            using (var client1 = await TestConnectionClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client2 = await TestConnectionClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client3 = await TestConnectionClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client4 = await TestConnectionClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
                await Task.WhenAll(client2.JoinGroup("test"), client4.JoinGroup("test"), client3.JoinGroup("test"));

                await client3.LeaveGroup("test");

                await client1.SendToGroup("test", "Hello, World!");

                AssertMessage(client1.Connection.ConnectionId, "Hello, World!", await client2.WaitForMessage());
                AssertMessage(client1.Connection.ConnectionId, "Hello, World!", await client4.WaitForMessage());
                AssertNoMessage(client1);
                AssertNoMessage(client3);
            }
        }

        [Theory]
        [MemberData(nameof(AllTransports))]
        public async Task Can_broadcast_to_group_joined_on_connection(ITransportFactory transportFactory)
        {
            using (var client1 = await TestConnectionClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client2 = await TestConnectionClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            using (var client3 = await TestConnectionClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
                await client1.SendToGroup("allclients", "Hello, Group!");

                AssertMessage(client1.Connection.ConnectionId, "Hello, Group!", await client3.WaitForMessage());
                AssertMessage(client1.Connection.ConnectionId, "Hello, Group!", await client2.WaitForMessage());
                AssertMessage(client1.Connection.ConnectionId, "Hello, Group!", await client1.WaitForMessage());
            }
        }

        [Theory]
        [MemberData(nameof(AllTransports))]
        public async Task Can_recover_from_reconnect(ITransportFactory transportFactory)
        {
            var reconnectTcs = new TaskCompletionSource<int>();
            var transport = transportFactory.Create();
            using (var client1 = await TestConnectionClient.Connect(_fixture.ServerInfo, transport))
            using (var client2 = await TestConnectionClient.Connect(_fixture.ServerInfo, transportFactory.Create()))
            {
                client1.Connection.Reconnected += () => reconnectTcs.SetResult(1);

                transport.LostConnection(client1.Connection);

                Assert.Equal(1, await reconnectTcs.Task);

                await client2.Broadcast("Hello!");

                AssertMessage(client2.Connection.ConnectionId, "Hello!", await client1.WaitForMessage());
            }
        }

        private void AssertMessage(string from, string value, TestConnectionMessage message)
        {
            Assert.Equal(MessageType.Message, message.Type);
            Assert.Equal(from, message.SourceOrDest);
            Assert.Equal(value, message.Value);
        }

        private void AssertNoMessage(TestConnectionClient client)
        {
            if (client.HasMessage())
            {
                var message = client.WaitForMessage().Result;
                Assert.False(true, $"Expected the client to not have received any message, but instead it has received a '{message.Type}' message containing '{message.Value}' from '{message.SourceOrDest}'");
            }
        }
    }
}

#endif