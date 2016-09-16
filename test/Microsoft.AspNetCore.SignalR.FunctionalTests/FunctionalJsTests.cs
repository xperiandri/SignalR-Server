using System;
using Microsoft.AspNetCore.SignalR.Testing.Common;
using Microsoft.AspNetCore.SignalR.Tests;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.FunctionalTests
{
    [Collection(FunctionalTestsCollection.Name)]
    public class FunctionalJsTests
    {
        ServerFixture _serverFixture;

        public FunctionalJsTests(FunctionalTestsServerFixture serverFixture)
        {
            _serverFixture = serverFixture;
        }

        [Fact]
        public void Run_javascript_functional_tests()
        {
            var exitCode =
                Utils.RunPhantomJS(_serverFixture.BaseUrl + "functionalTests.html",
                (s, e) => Console.WriteLine(e.Data), (s, e) => Console.WriteLine(e.Data));
            Assert.Equal(0, exitCode);
        }
    }
}