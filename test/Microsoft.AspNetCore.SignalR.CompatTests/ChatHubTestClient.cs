using System;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    public class ChatHubTestClient : IDisposable
    {
        private TaskCompletionSource<ChatHubMessage> _receivedAMessage = new TaskCompletionSource<ChatHubMessage>();

        public HubConnection Connection { get; }
        public IHubProxy Proxy { get; }

        public ChatHubTestClient(HubConnection connection, IHubProxy proxy)
        {
            Connection = connection;
            Proxy = proxy;

            Proxy.On("ReceiveMessage", (string from, string message) =>
            {
                var m = new ChatHubMessage(from, message);
                _receivedAMessage.TrySetResult(m);
            });
        }

        public static async Task<ChatHubTestClient> Connect(string hubsUrl, IClientTransport transport)
        {
            var client = new HubConnection(hubsUrl);
            var proxy = client.CreateHubProxy("ChatHub");
            await client.Start(transport);
            Assert.Equal(ConnectionState.Connected, client.State);
            return new ChatHubTestClient(client, proxy);
        }

        public Task SetName(string name) => Proxy.Invoke("SetName", name);
        public Task Broadcast(string message) => Proxy.Invoke("Broadcast", message);
        public Task<int> Add(int x, int y) => Proxy.Invoke<int>("Add", x, y);
        public Task<int> AddAsync(int x, int y) => Proxy.Invoke<int>("AddAsync", x, y);

        public Task JoinGroup(string name) => Proxy.Invoke("JoinGroup", name);
        public Task LeaveGroup(string name) => Proxy.Invoke("LeaveGroup", name);
        public Task SendToGroup(string group, string message) => Proxy.Invoke("SendToGroup", group, message);

        public bool HasMessage() => _receivedAMessage.Task.IsCompleted;

        public async Task<ChatHubMessage> WaitForMessage(int timeoutInMilliseconds = 5000)
        {
            var completed = await Task.WhenAny(Task.Delay(timeoutInMilliseconds), _receivedAMessage.Task);
            Assert.True(completed == _receivedAMessage.Task, "Receive timed out!");
            return _receivedAMessage.Task.Result;
        }

        public void Dispose()
        {
            Connection.Dispose();
        }
    }
}
