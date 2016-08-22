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

        public void SetName(string name)
        {
            Clients.CallerState.Username = name;
        }

        public void JoinGroup(string name)
        {
            Groups.Add(Context.ConnectionId, name);
        }

        public void LeaveGroup(string name)
        {
            Groups.Remove(Context.ConnectionId, name);
        }

        public void Broadcast(string message)
        {
            var name = Clients.CallerState.Username ?? "unknown";

            // Only send if the client has set a name
            if (!string.IsNullOrEmpty(name))
            {
                Clients.All.ReceiveMessage(name, message);
            }
        }

        public void SendToGroup(string group, string message)
        {
            var name = Clients.CallerState.Username ?? "unknown";

            // Only send if the client has set a name
            if (!string.IsNullOrEmpty(name))
            {
                Clients.Group(group).ReceiveMessage(name, message);
            }
        }
    }
}
