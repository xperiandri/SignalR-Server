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
        public void Protocol_resolves_correctly(string minProtocol, string maxProtocol, string clientProtocol, string expectedProtocolVersion)
        {
            var minProtocolVersion = new Version(minProtocol);
            var maxProtocolVersion = new Version(maxProtocol);
            var protocolResolver = new ProtocolResolver(minProtocolVersion, maxProtocolVersion);

            var reolvedVersion = protocolResolver.Resolve(clientProtocol);
            Assert.Equal(new Version(expectedProtocolVersion), reolvedVersion);
        }

        [Fact]
        public void Default_MinProtocolVersion_is_1_2()
        {
            var resolvedVersion = new ProtocolResolver().Resolve(null);
            Assert.Equal(new Version(1, 2), resolvedVersion);
        }

        [Fact]
        public void Default_MaxProtocolVersion_is_1_6()
        {
            var resolvedVersion = new ProtocolResolver().Resolve($"{int.MaxValue}.{int.MaxValue}");
            Assert.Equal(new Version(1, 6), resolvedVersion);
        }

        [Theory]
        [InlineData("1.2")]
        [InlineData("1.3")]
        [InlineData("1.4")]
        [InlineData("1.5")]
        [InlineData("1.6")]
        [InlineData("1.7")]
        public void IsClientProtocolEqualOrNewer_returns_correct_value(string clientProtocol)
        {
            var equalOrNewerThan1_4 = string.CompareOrdinal(clientProtocol, "1.4") >= 0;
            Assert.Equal(equalOrNewerThan1_4,
                new ProtocolResolver().IsClientProtocolEqualOrNewer(clientProtocol, ProtocolResolver.ProtocolVersion_1_4));
        }
    }
}
