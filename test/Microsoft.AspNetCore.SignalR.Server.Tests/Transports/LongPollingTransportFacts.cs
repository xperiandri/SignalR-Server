using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Infrastructure;
using Microsoft.AspNetCore.SignalR.Json;
using Microsoft.AspNetCore.SignalR.Transports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNetCore.SignalR.Tests.Transports
{
    public class LongPollingTransportFacts
    {
        [Fact]
        public void SupressReconnectsForRequestsNotEndingInReconnect()
        {
            // Arrange transports while specifying request paths
            var reconnectTransport = TestLongPollingTransport.Create("/reconnect");
            var pollTransport = TestLongPollingTransport.Create("/poll");
            var emptyPathTransport = TestLongPollingTransport.Create("/");

            // Assert
            Assert.False(reconnectTransport.TestSuppressReconnect);
            Assert.True(pollTransport.TestSuppressReconnect);
            Assert.True(emptyPathTransport.TestSuppressReconnect);
        }

        [Fact]
        public void EmptyPathDoesntTriggerReconnects()
        {
            // Arrange
            var transport = TestLongPollingTransport.Create(requestPath: "/");

            var connected = false;
            var reconnected = false;

            transport.Connected = () =>
            {
                connected = true;
                return TaskAsyncHelper.Empty;
            };

            transport.Reconnected = () =>
            {
                reconnected = true;
                return TaskAsyncHelper.Empty;
            };

            // Act
            transport.ProcessRequest(CreateMockTransportConnection());

            // Assert
            Assert.True(transport.ConnectTask.Wait(TimeSpan.FromSeconds(2)), "ConnectTask task not tripped");
            Assert.False(connected, "The Connected event should not be raised");
            Assert.False(reconnected, "The Reconnected event should not be raised");
        }

        [Fact]
        public void SetTheCorrectMIMETypeForJSONSends()
        {
            // Arrange
            var transport = TestLongPollingTransport.Create("/send");

            // Act
            transport.Send(new object());

            // Assert
            Assert.True(transport.TestContentType.Wait(TimeSpan.FromSeconds(2)), "ContentType not set");
            Assert.Equal(JsonUtility.JsonMimeType, transport.TestContentType.Result);
        }

        [Fact]
        public void SetTheCorrectMIMETypeForJSONPSends()
        {
            // Arrange
            // Make the transport think it is responding to a JSONP request
            var queryString = new Dictionary<string, string> { { "callback", "foo" } };
            var transport = TestLongPollingTransport.Create("/send", queryString);

            // Act
            // JSONP send
            transport.Send(new object());

            // Assert
            Assert.True(transport.TestContentType.Wait(TimeSpan.FromSeconds(2)), "ContentType not set");
            Assert.Equal(JsonUtility.JavaScriptMimeType, transport.TestContentType.Result);
        }

        [Fact]
        public void SetTheCorrectMIMETypeForJSONPolls()
        {
            // Arrange
            var transport = TestLongPollingTransport.Create("/poll");

            // Act
            transport.ProcessRequest(CreateMockTransportConnection());

            // Assert
            Assert.True(transport.TestContentType.Wait(TimeSpan.FromSeconds(2)), "ContentType not set");
            Assert.Equal(JsonUtility.JsonMimeType, transport.TestContentType.Result);
        }

        [Fact]
        public void SetTheCorrectMIMETypeForJSONPPolls()
        {
            // Arrange
            // Make the transport think it is responding to a JSONP request
            var queryString = new Dictionary<string, string> { { "callback", "foo" } };
            var transport = TestLongPollingTransport.Create("/poll", queryString);

            // Act
            transport.ProcessRequest(CreateMockTransportConnection());

            // Assert
            Assert.True(transport.TestContentType.Wait(TimeSpan.FromSeconds(2)), "ContentType not set");
            Assert.Equal(JsonUtility.JavaScriptMimeType, transport.TestContentType.Result);
        }

        [Theory]
        [InlineData(false, "foo", null, "foo")]
        [InlineData(false, "foo", "bar", "foo")]
        [InlineData(false, null, "bar", null)]
        [InlineData(true, "foo", null, "foo")]
        [InlineData(true, "foo", "bar", "foo")]
        [InlineData(true, null, "bar", "bar")]
        public async Task VerifyGroupsTokenReadCorrectly(bool hasFormContentType, string queryStringGroupsToken,
            string formGroupsToken, string expectedGroupsToken)
        {
            var queryString = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(queryStringGroupsToken))
            {
                queryString.Add("groupsToken", queryStringGroupsToken);
            }

            var form = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(formGroupsToken))
            {
                form.Add("groupsToken", formGroupsToken);
            }

            var testContext = new TestContext("/poll", queryString, form);
            testContext.MockRequest.SetupGet(r => r.HasFormContentType).Returns(hasFormContentType);

            var longPollingTransport = TestLongPollingTransport.Create(testContext);

            var groupsToken = await longPollingTransport.GetGroupsToken();

            Assert.Equal(expectedGroupsToken, groupsToken);
        }

        [Theory]
        [InlineData(false, "foo", null, "foo")]
        [InlineData(false, "foo", "bar", "foo")]
        [InlineData(false, null, "bar", null)]
        [InlineData(true, "foo", null, "foo")]
        [InlineData(true, "foo", "bar", "foo")]
        [InlineData(true, null, "bar", "bar")]
        public async Task VerifyMessageIdReadCorrectly(bool hasFormContentType, string queryStringMessageId,
            string formMessageId, string expectedMessageId)
        {
            var queryString = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(queryStringMessageId))
            {
                queryString.Add("messageId", queryStringMessageId);
            }

            var form = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(formMessageId))
            {
                form.Add("messageId", formMessageId);
            }

            var testContext = new TestContext("/poll", queryString, form);
            testContext.MockRequest.SetupGet(r => r.HasFormContentType).Returns(hasFormContentType);

            var longPollingTransport = TestLongPollingTransport.Create(testContext);

            await longPollingTransport.InitializeMessageId();

            Assert.Equal(expectedMessageId, longPollingTransport.LastMessageId);
        }

        private static ITransportConnection CreateMockTransportConnection()
        {
            var transportConnection = new Mock<ITransportConnection>();
            transportConnection.Setup(m => m.Receive(It.IsAny<string>(),
                                                     It.IsAny<Func<PersistentResponse, object, Task<bool>>>(),
                                                     It.IsAny<int>(),
                                                     It.IsAny<object>())).Returns(DisposableAction.Empty);
            return transportConnection.Object;
        }

        private class TestLongPollingTransport : LongPollingTransport
        {
            private TaskCompletionSource<string> _contentTypeTcs = new TaskCompletionSource<string>();

            public TestLongPollingTransport(HttpContext context,
                                            JsonSerializer jsonSerializer,
                                            ITransportHeartbeat heartbeat,
                                            IPerformanceCounterManager performanceCounterManager,
                                            IApplicationLifetime applicationLifetime,
                                            ILoggerFactory loggerFactory,
                                            IOptions<SignalROptions> optionsAccessor,
                                            IMemoryPool pool)
                : base(context, jsonSerializer, heartbeat, performanceCounterManager, applicationLifetime, loggerFactory, optionsAccessor, pool)
            {

            }

            public static TestLongPollingTransport Create(
                string requestPath,
                Dictionary<string, string> queryString = null)
            {
                return Create(new TestContext(requestPath, queryString));
            }

            public static TestLongPollingTransport Create(TestContext testContext)
            {
                TestLongPollingTransport transport = null;

                testContext.MockResponse.SetupSet(m => m.ContentType = It.IsAny<string>()).Callback<string>(contentType =>
                {
                    transport._contentTypeTcs.SetResult(contentType);
                });

                var json = JsonUtility.CreateDefaultSerializer();
                var heartBeat = new Mock<ITransportHeartbeat>();
                var counters = new Mock<IPerformanceCounterManager>();
                var appLifetime = new Mock<IApplicationLifetime>();
                var loggerFactory = new Mock<ILoggerFactory>();
                var optionsAccessor = new Mock<IOptions<SignalROptions>>();
                optionsAccessor.Setup(m => m.Value).Returns(new SignalROptions());
                var pool = new Mock<IMemoryPool>();

                transport = new TestLongPollingTransport(
                    testContext.MockHttpContext.Object,
                    json,
                    heartBeat.Object,
                    counters.Object,
                    appLifetime.Object,
                    loggerFactory.Object,
                    optionsAccessor.Object,
                    pool.Object);

                return transport;
            }

            public Task<string> TestContentType
            {
                get { return _contentTypeTcs.Task; }
            }

            public bool TestSuppressReconnect
            {
                get { return SuppressReconnect; }
            }

            public new async Task InitializeMessageId()
            {
                await base.InitializeMessageId();
            }

            public new string LastMessageId
            {
                get { return base.LastMessageId; }
            }
        }
    }
}
