#if NET451

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.AspNet.SignalR.Client.Http;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    // Really just here so that it can be used in xunit Theories and have a good "ToString" (Func<T> does not :( have a good ToString...)

    public interface ITransportFactory
    {
        IClientTransport Create();
    }

    public static class TransportFactory
    {
        public static IEnumerable<ITransportFactory[]> All
        {
            get
            {
                if (WebSocketsSupported())
                {
                    yield return new[] { new TransportFactory<WebSocketTransport>() };
                }
                yield return new[] { new TransportFactory<LongPollingTransport>() };
                yield return new[] { new TransportFactory<ServerSentEventsTransport>() };
            }
        }

        private static bool WebSocketsSupported()
        {
            // We don't have cross-plat WebSockets yet, and the current implementation requires Win8+
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                !Microsoft.Extensions.Internal.RuntimeEnvironment.OperatingSystemVersion.StartsWith("6.1");
        }
    }

    public class TransportFactory<T> : ITransportFactory
        where T : IClientTransport, new()
    {
        private Func<T> _factory;

        public TransportFactory()
        {
            _factory = () => new T();
        }

        public IClientTransport Create()
        {
            return _factory();
        }

        public override string ToString() => typeof(T).Name;
        public override int GetHashCode() => typeof(T).GetHashCode();
    }
}

#endif