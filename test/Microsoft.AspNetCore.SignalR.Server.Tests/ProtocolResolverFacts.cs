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
        public void Protocol_resolves_correctly(string minProtocol, string maxProtocol, string clientProtocol, string expectedProtocol)
        {
            var minProtocolVersion = new Version(minProtocol);
            var maxProtocolVersion = new Version(maxProtocol);
            var protocolResolver = new ProtocolResolver(minProtocolVersion, maxProtocolVersion);

            var version = protocolResolver.Resolve(clientProtocol);
            Assert.Equal(new Version(expectedProtocol), version);
        }

        [Fact]
        public void Default_MinProtocolVersion_is_1_2()
        {
            Assert.Equal(new Version(1, 2), protocolResolver.Resolve(null));
        }

        [Fact]
        public void Default_MaxProtocolVersion_is_1_6()
        {
            Assert.Equal(new Version(1, 6), protocolResolver.Resolve($"{int.MaxValue}.{int.MaxValue}"));
        }

        [Theory]
        [InlineData("1.2", false)]
        [InlineData("1.3", false)]
        [InlineData("1.4", true)]
        [InlineData("1.5", true)]
        [InlineData("1.6", true)]
        public void Delayed_start_supported_in_1_4_and_above(string clientProtocol, bool expectedSupportsDelayedStart)
        {
            Assert.Equal(expectedSupportsDelayedStart, protocolResolver.SupportsDelayedStart(clientProtocol));
        }
    }
}
