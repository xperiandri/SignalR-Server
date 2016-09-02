using System;
using Microsoft.AspNetCore.SignalR.Tests;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    [CollectionDefinition(Name)]
    public class CompatTestsCollection : ICollectionFixture<CompatTestsServerFixture>
    {
        public const string Name = "CompatTests";
    }

    public class CompatTestsServerFixture : ServerFixture
    {
        public ServerInfo ServerInfo => new ServerInfo(BaseUrl);

        protected override string ServerProjectName
        {
            get
            {
                return "Microsoft.AspNetCore.SignalR.Test.Server";
            }
        }
    }
}
