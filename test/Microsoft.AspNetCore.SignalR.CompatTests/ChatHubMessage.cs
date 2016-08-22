namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    public class ChatHubMessage
    {
        public string From { get; }
        public string Message { get; }

        public ChatHubMessage(string from, string message)
        {
            From = from;
            Message = message;
        }
    }
}