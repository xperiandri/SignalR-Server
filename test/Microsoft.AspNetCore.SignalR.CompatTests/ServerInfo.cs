namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    public class ServerInfo
    {
        public string ApplicationBaseUri { get; }
        public string HubConnectionUrl => ApplicationBaseUri + "/test/hubs";

        public ServerInfo(string applicationBaseUri)
        {
            ApplicationBaseUri = applicationBaseUri;
            if(ApplicationBaseUri.EndsWith("/"))
            {
                ApplicationBaseUri = ApplicationBaseUri.Substring(0, ApplicationBaseUri.Length - 1);
            }
        }
    }
}