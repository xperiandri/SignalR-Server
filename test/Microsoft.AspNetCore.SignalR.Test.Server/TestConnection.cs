using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.CompatTests.Server
{
    public class TestConnection : PersistentConnection
    {
        protected override async Task OnConnected(HttpRequest request, string connectionId)
        {
            await Groups.Add(connectionId, "allclients");
            await base.OnConnected(request, connectionId);
        }

        protected override async Task OnReceived(HttpRequest request, string connectionId, string data)
        {
            var message = JsonConvert.DeserializeObject<Message>(data);

            switch (message.Type)
            {
                case MessageType.JoinGroup:
                    await Groups.Add(connectionId, message.Value);
                    await Connection.Send(connectionId, $"+{message.Value}");
                    break;
                case MessageType.LeaveGroup:
                    await Groups.Remove(connectionId, message.Value);
                    await Connection.Send(connectionId, $"-{message.Value}");
                    break;
                case MessageType.Broadcast:
                    await Connection.Broadcast(new Message
                    {
                        Type = MessageType.Message,
                        SourceOrDest = connectionId,
                        Value = message.Value
                    });
                    break;
                case MessageType.SendToGroup:
                    await Groups.Send(message.SourceOrDest, new Message
                    {
                        Type = MessageType.Message,
                        SourceOrDest = connectionId,
                        Value = message.Value
                    });
                    break;
                default:
                    throw new InvalidOperationException("Invalid message type");
            }

            await base.OnReceived(request, connectionId, data);
        }

        enum MessageType
        {
            JoinGroup = 0,
            LeaveGroup = 1,
            Broadcast = 2,
            SendToGroup = 3,
            Message = 4
        }

        class Message
        {
            public MessageType Type { get; set; }
            public string SourceOrDest { get; set; }
            public string Value { get; set; }
        }
    }
}
