using System;
using Microsoft.AspNetCore.SignalR.Testing.Common;
using Microsoft.AspNetCore.SignalR.Tests;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    [Collection(CompatTestsCollection.Name)]
    public class CompatJsTests
    {
        ServerFixture _serverFixture;

        public CompatJsTests(CompatTestsServerFixture serverFixture)
        {
            _serverFixture = serverFixture;
        }

        [Fact]
        public void Run_javascript_compat_tests()
        {
            string stdOut, stdErr;
            var exitCode =
                Utils.RunPhantomJS(_serverFixture.BaseUrl + "compatTests.html", out stdOut, out stdErr);
            Console.WriteLine(stdOut);
            Console.WriteLine(stdErr);
            Assert.Equal(0, exitCode);
        }
    }
}