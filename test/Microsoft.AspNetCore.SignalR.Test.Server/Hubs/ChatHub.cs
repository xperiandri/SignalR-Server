using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SignalR.CompatTests.Server
{
    public class ChatHub : Hub
    {
        public override async Task OnConnected()
        {
            // TODO: investigate - this causes a random hangs when using long polling transport and JS Client
            // await Groups.Add(Context.ConnectionId, "onconnectedgroup");
            await base.OnConnected();
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

        public async Task JoinGroup(string name)
        {
            await Groups.Add(Context.ConnectionId, name);
        }

        public async Task LeaveGroup(string name)
        {
            await Groups.Remove(Context.ConnectionId, name);
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
                await Task.Delay(100);
            }
        }
    }
}
