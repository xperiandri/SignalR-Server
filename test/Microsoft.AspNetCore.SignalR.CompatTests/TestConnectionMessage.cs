namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    public enum MessageType
    {
        JoinGroup,
        LeaveGroup,
        Broadcast,
        SendToGroup,
        Message
    }

    public class TestConnectionMessage
    {
        public MessageType Type { get; set; }
        public string SourceOrDest { get; set; }
        public string Value { get; set; }

    }
}
