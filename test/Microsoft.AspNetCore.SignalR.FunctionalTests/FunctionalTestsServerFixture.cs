using Microsoft.AspNetCore.SignalR.Tests;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.FunctionalTests
{
    [CollectionDefinition(Name)]
    public class FunctionalTestsCollection : ICollectionFixture<FunctionalTestsServerFixture>
    {
        public const string Name = "FunctionalTests";
    }

    public class FunctionalTestsServerFixture : ServerFixture
    {
        protected override string ServerProjectName
        {
            get
            {
                return "Microsoft.AspNetCore.SignalR.Test.Server";
            }
        }
    }
}
