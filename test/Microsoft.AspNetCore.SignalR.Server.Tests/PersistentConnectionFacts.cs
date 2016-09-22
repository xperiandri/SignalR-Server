using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using Microsoft.AspNetCore.SignalR.Transports;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests
{
    public class PersistentConnectionFacts
    {
        public class ProcessRequest
        {
            [Fact]
            public void UnknownTransportFails()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var qs = new Dictionary<string, string>();
                var sp = ServiceProviderHelper.CreateServiceProvider();
                var context = new TestContext("/", qs);
                context.MockResponse.SetupProperty(r => r.StatusCode);
                connection.Object.Initialize(sp);

                var task = connection.Object.ProcessRequest(context.MockHttpContext.Object);

                Assert.True(task.IsCompleted);
                Assert.Equal(400, context.MockResponse.Object.StatusCode);
            }

            [Fact]
            public void MissingConnectionTokenFails()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var qs = new Dictionary<string, string>();
                var sp = ServiceProviderHelper.CreateServiceProvider();
                var context = new TestContext("/", qs);
                context.MockResponse.SetupProperty(r => r.StatusCode);
                connection.Object.Initialize(sp);

                var task = connection.Object.ProcessRequest(context.MockHttpContext.Object);

                Assert.True(task.IsCompleted);
                Assert.Equal(400, context.MockResponse.Object.StatusCode);
            }

            [Fact]
            public void UncleanDisconnectFiresOnDisconnected()
            {
                // Arrange
                var context = new TestContext("/", new Dictionary<string, string> { { "connectionToken", "1" } });

                var transport = new Mock<ITransport>();
                transport.SetupProperty(m => m.Disconnected);
                transport.SetupProperty(m => m.ConnectionId);
                transport.Setup(m => m.GetGroupsToken()).Returns(TaskAsyncHelper.FromResult(string.Empty));
                transport.Setup(m => m.ProcessRequest(It.IsAny<Connection>())).Returns(TaskAsyncHelper.Empty);

                var transportManager = new Mock<ITransportManager>();
                transportManager.Setup(m => m.GetTransport(context.MockHttpContext.Object)).Returns(transport.Object);

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns<string, string>((value, purpose) =>  value);

                var connection = new TestablePersistentConnection();

                var sp = ServiceProviderHelper.CreateServiceProvider(services =>
                {
                    services.AddSingleton<ITransportManager>(transportManager.Object);
                    services.AddSingleton<IProtectedData>(protectedData.Object);
                });

                connection.Initialize(sp);

                // Act
                connection.ProcessRequest(context.MockHttpContext.Object).Wait();
                transport.Object.Disconnected(/* clean: */ false);

                // Assert
                Assert.True(connection.OnDisconectedCalled);
            }
        }

        public class TestablePersistentConnection : PersistentConnection
        {
            public bool OnDisconectedCalled { get; private set; }

            protected override Task OnDisconnected(HttpRequest request, string connectionId, bool stopCalled)
            {
                OnDisconectedCalled = true;
                return Task.FromResult(0);
            }
        }

        public class VerifyGroups
        {
            [Fact]
            public void MissingGroupTokenReturnsEmptyList()
            {
                var groups = DoVerifyGroups(groupsToken: null, connectionId: null);

                Assert.Equal(0, groups.Count);
            }

            [Fact]
            public void NullProtectedDataTokenReturnsEmptyList()
            {
                var groups = DoVerifyGroups(groupsToken: "groups", connectionId: null, hasProtectedData: false);

                Assert.Equal(0, groups.Count);
            }

            [Fact]
            public void GroupsTokenWithInvalidConnectionIdReturnsEmptyList()
            {
                var groups = DoVerifyGroups(groupsToken: @"wrong:[""g1"",""g2""]", connectionId: "id");

                Assert.Equal(0, groups.Count);
            }

            [Fact]
            public void GroupsAreParsedFromToken()
            {
                var groups = DoVerifyGroups(groupsToken: @"id:[""g1"",""g2""]", connectionId: "id");

                Assert.Equal(2, groups.Count);
                Assert.Equal("g1", groups[0]);
                Assert.Equal("g2", groups[1]);
            }

            private static IList<string> DoVerifyGroups(string groupsToken, string connectionId, bool hasProtectedData = true)
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var qs = new Dictionary<string, string>();
                var context = new TestContext("/", qs);
                context.MockResponse.SetupProperty(r => r.StatusCode);
                qs["transport"] = "serverSentEvents";
                qs["connectionToken"] = "1";
                qs["groupsToken"] = groupsToken;

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);

                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>()))
                             .Returns<string, string>((value, purpose) => hasProtectedData ? value : null);

                var sp = ServiceProviderHelper.CreateServiceProvider(services =>
                {
                    services.AddSingleton<IProtectedData>(protectedData.Object);
                });

                connection.Object.Initialize(sp);

                return connection.Object.VerifyGroups(connectionId, groupsToken);
            }
        }

        public class GetConnectionId
        {
            [Fact]
            public void UnprotectedConnectionTokenFails()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var context = new TestContext("/");

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);
                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>()))
                             .Throws<InvalidOperationException>();

                var sp = ServiceProviderHelper.CreateServiceProvider(services =>
                {
                    services.AddSingleton<IProtectedData>(protectedData.Object);
                });

                connection.Object.Initialize(sp);

                string connectionId;
                string message;
                int statusCode;

                Assert.Equal(false, connection.Object.TryGetConnectionId(context.MockHttpContext.Object, "1", out connectionId, out message, out statusCode));
                Assert.Equal(null, connectionId);
                Assert.Equal(400, statusCode);
            }

            [Fact]
            public void NullUnprotectedConnectionTokenFails()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var context = new TestContext("/");

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);
                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>())).Returns((string)null);

                var sp = ServiceProviderHelper.CreateServiceProvider(services =>
                {
                    services.AddSingleton<IProtectedData>(protectedData.Object);
                });

                connection.Object.Initialize(sp);

                string connectionId;
                string message;
                int statusCode;

                Assert.Equal(false, connection.Object.TryGetConnectionId(context.MockHttpContext.Object, "1", out connectionId, out message, out statusCode));
                Assert.Equal(null, connectionId);
                Assert.Equal(400, statusCode);
            }

            [Fact]
            public void UnauthenticatedUserWithAuthenticatedTokenFails()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var context = new TestContext("/");

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);
                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((value, purpose) => value);

                var sp = ServiceProviderHelper.CreateServiceProvider(services =>
                {
                    services.AddSingleton<IProtectedData>(protectedData.Object);
                });

                connection.Object.Initialize(sp);

                string connectionId;
                string message;
                int statusCode;

                Assert.Equal(false, connection.Object.TryGetConnectionId(context.MockHttpContext.Object, "1:::11:::::::1:1", out connectionId, out message, out statusCode));
                Assert.Equal(403, statusCode);
            }

            [Fact]
            public void AuthenticatedUserNameMatches()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var context = new TestContext("/");
                context.MockHttpContext.Setup(m => m.User)
                       .Returns(new ClaimsPrincipal(new GenericIdentity("Name")));

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);
                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((value, purpose) => value);

                var sp = ServiceProviderHelper.CreateServiceProvider(services =>
                {
                    services.AddSingleton<IProtectedData>(protectedData.Object);
                });

                connection.Object.Initialize(sp);

                string connectionId;
                string message;
                int statusCode;

                Assert.Equal(true, connection.Object.TryGetConnectionId(context.MockHttpContext.Object, "1:Name", out connectionId, out message, out statusCode));
                Assert.Equal("1", connectionId);
            }

            [Fact]
            public void AuthenticatedUserWithColonsInUserName()
            {
                var connection = new Mock<PersistentConnection>() { CallBase = true };
                var context = new TestContext("/");
                context.MockHttpContext.Setup(m => m.User)
                       .Returns(new ClaimsPrincipal(new GenericIdentity("::11:::::::1:1")));

                string connectionId = Guid.NewGuid().ToString("d");

                var protectedData = new Mock<IProtectedData>();
                protectedData.Setup(m => m.Protect(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns<string, string>((value, purpose) => value);
                protectedData.Setup(m => m.Unprotect(It.IsAny<string>(), It.IsAny<string>())).Returns<string, string>((value, purpose) => value);

                var sp = ServiceProviderHelper.CreateServiceProvider(services =>
                {
                    services.AddSingleton<IProtectedData>(protectedData.Object);
                });

                connection.Object.Initialize(sp);

                string cid;
                string message;
                int statusCode;

                Assert.Equal(true, connection.Object.TryGetConnectionId(context.MockHttpContext.Object, connectionId + ":::11:::::::1:1", out cid, out message, out statusCode));
                Assert.Equal(connectionId, cid);
            }

            [Fact]
            public async Task Negotiate_returns_supported_transports_as_array_for_client_protocol_1_6()
            {
                var qs = new Dictionary<string, string> { { "clientProtocol", "1.6" } };
                var sp = ServiceProviderHelper.CreateServiceProvider();
                var context = new TestContext("/negotiate", qs);

                var connection = new Mock<PersistentConnection> { CallBase = true }.Object;
                connection.Initialize(sp);
                await connection.ProcessRequest(context.MockHttpContext.Object);

                Assert.NotEmpty(context.ResponseBuffer);
                var negotiateJson = JObject.Parse(context.ResponseBuffer[0]);
                Assert.IsType(typeof(JArray), negotiateJson["Transports"]);
                Assert.Null(negotiateJson["TryWebSockets"]);
            }

            [Fact]
            public async Task Negotiate_does_not_include_excluded_transports()
            {
                var qs = new Dictionary<string, string> { { "clientProtocol", "1.6" } };
                var context = new TestContext("/negotiate", qs);
                context.MockHttpContext.Setup(m => m.Features.Get<IHttpWebSocketFeature>())
                    .Returns(Mock.Of<IHttpWebSocketFeature>());

                var mockTransportManager = new Mock<ITransportManager>();

                mockTransportManager
                    .Setup(m => m.SupportsTransport(It.IsIn(new[] { "webSockets", "longPolling" })))
                    .Returns(true);

                var sp = ServiceProviderHelper.CreateServiceProvider(services =>
                {
                    services.AddSingleton(mockTransportManager.Object);
                });

                var connection = new Mock<PersistentConnection> { CallBase = true }.Object;
                connection.Initialize(sp);
                await connection.ProcessRequest(context.MockHttpContext.Object);

                Assert.NotEmpty(context.ResponseBuffer);
                var negotiateJson = JObject.Parse(context.ResponseBuffer[0]);
                Assert.Equal(
                    new[] { "webSockets", "longPolling" },
                    negotiateJson["Transports"].Select(i => (string)i));
            }

            [Fact]
            public async Task Negotiate_does_not_return_supported_transports_for_client_protocol_1_5()
            {
                var qs = new Dictionary<string, string> { { "clientProtocol", "1.5" } };
                var sp = ServiceProviderHelper.CreateServiceProvider();
                var context = new TestContext("/negotiate", qs);

                var connection = new Mock<PersistentConnection> { CallBase = true }.Object;
                connection.Initialize(sp);
                await connection.ProcessRequest(context.MockHttpContext.Object);

                Assert.NotEmpty(context.ResponseBuffer);
                var negotiateJson = JObject.Parse(context.ResponseBuffer[0]);
                Assert.NotNull(negotiateJson["TryWebSockets"]);
                Assert.False((bool)negotiateJson["TryWebSockets"]);
                Assert.Null(negotiateJson["Transports"]);
            }
        }
    }
}
