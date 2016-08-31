using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    public class TestConnectionClient : IDisposable
    {
        private TaskCompletionSource<TestConnectionMessage> _message = new TaskCompletionSource<TestConnectionMessage>();

        public Connection Connection { get; private set; }

        public TestConnectionClient(Connection connection)
        {
            Connection = connection;
            Connection.Received += Connection_Received;
        }

        private void Connection_Received(string obj)
        {
            var message = JsonConvert.DeserializeObject<TestConnectionMessage>(obj);
            _message.SetResult(message);
        }

        public static async Task<TestConnectionClient> Connect(ServerInfo server, IClientTransport transport)
        {
            var connection = new Connection(server.RawConnectionUrl);
            await connection.Start(transport);
            return new TestConnectionClient(connection);
        }

        public void Dispose()
        {
            if (Connection != null)
            {
                Connection.Dispose();
                Connection = null;
            }
        }

        public bool HasMessage() => _message.Task.IsCompleted;

        public Task Broadcast(string message) => Send(MessageType.Broadcast, message);

        public Task JoinGroup(string name) => Send(MessageType.JoinGroup, name);

        public Task LeaveGroup(string name) => Send(MessageType.LeaveGroup, name);

        public Task SendToGroup(string group, string message) => Send(MessageType.SendToGroup, group, message);

        public Task Send(MessageType type, string value) => Send(type, dest: null, value: value);

        public Task Send(MessageType type, string dest, string value)
        {
            return Connection.Send(new TestConnectionMessage()
            {
                Type = type,
                SourceOrDest = dest,
                Value = value
            });
        }

        public async Task<TestConnectionMessage> WaitForMessage(int timeoutInMilliseconds = 5000)
        {
            var completed = await Task.WhenAny(Task.Delay(timeoutInMilliseconds), _message.Task);
            Assert.True(completed == _message.Task, "Receive timed out!");
            return _message.Task.Result;
        }
    }
}
