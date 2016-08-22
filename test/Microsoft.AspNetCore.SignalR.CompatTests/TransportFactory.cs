using System;
using Microsoft.AspNet.SignalR.Client.Transports;

namespace Microsoft.AspNetCore.SignalR.CompatTests
{
    // Really just here so that it can be used in xunit Theories and have a good "ToString" (Func<T> does not :( have a good ToString...) 

    public interface ITransportFactory
    {
        IClientTransport Create();
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
