using System;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class ProtocolResolverFacts
    {
        [Theory]
        [InlineData("1.0", "1.5", ".8", "1.0")]
        [InlineData("1.0", "1.5", "1.0", "1.0")]
        [InlineData("1.0", "1.5", "1.2.5", "1.2.5")]
        [InlineData("1.0", "1.5", "1.5", "1.5")]
        [InlineData("1.0", "1.5", "1.9", "1.5")]
        [InlineData("1.0", "1.1", "1.0.5", "1.0.5")]
        [InlineData("1.0", "1.1", "", "1.0")]
        [InlineData("1.0", "1.1", null, "1.0")]
        public void ProtocolResolvesCorrectly(string minProtocol, string maxProtocol, string clientProtocol, string expectedProtocol)
        {
            var minProtocolVersion = new Version(minProtocol);
            var maxProtocolVersion = new Version(maxProtocol);
            var protocolResolver = new ProtocolResolver(minProtocolVersion, maxProtocolVersion);

            var version = protocolResolver.Resolve(clientProtocol);

            Assert.Equal(version, new Version(expectedProtocol));
        }
    }
}
