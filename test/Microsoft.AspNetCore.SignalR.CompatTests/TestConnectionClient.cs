#if NET451

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
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
        private ConcurrentDictionary<string, TaskCompletionSource<int>> _pendingGroupOperations =
            new ConcurrentDictionary<string, TaskCompletionSource<int>>();

        private readonly StringBuilder _traceStringBuilder;

        public Connection Connection { get; private set; }

        public string Trace { get { return _traceStringBuilder.ToString(); } }

        public TestConnectionClient(Connection connection, StringBuilder traceStringBuilder)
        {
            Connection = connection;
            Connection.Received += Connection_Received;
            _traceStringBuilder = traceStringBuilder;
        }

        private void Connection_Received(string obj)
        {
            TaskCompletionSource<int> tcs;
            if (_pendingGroupOperations.TryRemove(obj, out tcs))
            {
                tcs.TrySetResult(0);
                return;
            }

            var message = JsonConvert.DeserializeObject<TestConnectionMessage>(obj);
            _message.SetResult(message);
        }

        public static async Task<TestConnectionClient> Connect(ServerInfo server, IClientTransport transport)
        {
            var connection = new Connection(server.RawConnectionUrl);
            var traceStringBuilder = new StringBuilder();
            connection.TraceWriter = new StringWriter(traceStringBuilder);
            connection.TraceLevel = TraceLevels.All;
            await connection.Start(transport);
            return new TestConnectionClient(connection, traceStringBuilder);
        }

        public void Dispose()
        {
            if (Connection != null)
            {
                Connection.Dispose();
                Connection = null;

                var values = _pendingGroupOperations.Values;
                _pendingGroupOperations.Clear();
                foreach (var tcs in values)
                {
                    tcs.TrySetCanceled();
                }

                _pendingGroupOperations.Clear();
            }
        }

        public bool HasMessage() => _message.Task.IsCompleted;

        public Task Broadcast(string message) => Send(MessageType.Broadcast, message);

        public async Task JoinGroup(string name)
        {
            var tcs = _pendingGroupOperations.GetOrAdd($"+{name}", _ => new TaskCompletionSource<int>());
            await Send(MessageType.JoinGroup, name);
            await tcs.Task;
        }

        public async Task LeaveGroup(string name)
        {
            var tcs = _pendingGroupOperations.GetOrAdd($"-{name}", _ => new TaskCompletionSource<int>());
            await Send(MessageType.LeaveGroup, name);
            await tcs.Task;

        }

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
            Assert.True(completed == _message.Task, "Receive timed out!" + Environment.NewLine + Trace);
            return _message.Task.Result;
        }
    }
}

#endif