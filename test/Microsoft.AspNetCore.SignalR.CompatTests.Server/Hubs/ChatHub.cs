using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.CompatTests.Server
{
    public class ChatHub : Hub
    {
        public override Task OnConnected()
        {
            Groups.Add(Context.ConnectionId, "onconnectedgroup");
            return base.OnConnected();
        }

        public int Add(int x, int y)
        {
            return x + y;
        }

        public async Task<int> AddAsync(int x, int y)
        {
            await Task.Delay(500);
            return x + y;
        }

        public void JoinGroup(string name)
        {
            Groups.Add(Context.ConnectionId, name);
        }

        public void LeaveGroup(string name)
        {
            Groups.Remove(Context.ConnectionId, name);
        }

        public void Broadcast(string sender, string message)
        {
            Clients.All.ReceiveMessage(sender, message);
        }

        public void SendToGroup(string sender, string group, string message)
        {
            Clients.Group(group).ReceiveMessage(sender, message);
        }

        public IEnumerable<int> CrashConnection()
        {
            yield return 1;

            throw new Exception("KABOOM!");
        }

        public async Task WithProgress(IProgress<int> progress)
        {
            for(int i = 0; i < 5; i++)
            {
                progress.Report(i);
                await Task.Delay(10);
            }
        }
    }
}
