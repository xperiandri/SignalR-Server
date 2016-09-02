using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNet.SignalR.Client.Transports;
using Microsoft.AspNetCore.SignalR.Tests;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    public class ChatHubTestClient : IDisposable
    {
        private TaskCompletionSource<ChatHubMessage> _receivedAMessage = new TaskCompletionSource<ChatHubMessage>();

        public HubConnection Connection { get; }
        public IHubProxy Proxy { get; }
        public IList<int> ReceivedProgress { get; }

        public ChatHubTestClient(HubConnection connection, IHubProxy proxy)
        {
            Connection = connection;
            Proxy = proxy;
            ReceivedProgress = new List<int>();

            Proxy.On("ReceiveMessage", (string from, string message) =>
            {
                var m = new ChatHubMessage(from, message);
                _receivedAMessage.TrySetResult(m);
            });
        }

        public static async Task<ChatHubTestClient> Connect(ServerInfo server, IClientTransport transport)
        {
            var client = new HubConnection(server.HubConnectionUrl);
            var proxy = client.CreateHubProxy("ChatHub");
            await client.Start(transport);
            return new ChatHubTestClient(client, proxy);
        }

        public Task Broadcast(string sender, string message) => Proxy.Invoke("Broadcast", sender, message);
        public Task<int> Add(int x, int y) => Proxy.Invoke<int>("Add", x, y);
        public Task<int> AddAsync(int x, int y) => Proxy.Invoke<int>("AddAsync", x, y);

        public Task JoinGroup(string name) => Proxy.Invoke("JoinGroup", name);
        public Task LeaveGroup(string name) => Proxy.Invoke("LeaveGroup", name);
        public Task SendToGroup(string sender, string group, string message) => Proxy.Invoke("SendToGroup", sender, group, message);
        public Task<IEnumerable<int>> CrashConnection() => Proxy.Invoke<IEnumerable<int>>("CrashConnection");
        public Task WithProgress() => Proxy.Invoke<int>(
            method: "WithProgress",
            onProgress: OnProgress,
            args: new object[0]);

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

        public void OnProgress(int value)
        {
            ReceivedProgress.Add(value);
        }
    }
}
