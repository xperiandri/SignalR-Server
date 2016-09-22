// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.SignalR
{
    internal class NegotiateResponsePayload
    {
        public string Url { get; set; }

        public string ConnectionToken { get; set; }

        public string ConnectionId { get; set; }

        public double? KeepAliveTimeout { get; set; }

        public double DisconnectTimeout { get; set; }

        public double ConnectionTimeout { get; set; }

        public bool? TryWebSockets { get; set; }

        public bool ShouldSerializeTryWebSockets() => TryWebSockets != null;

        public IEnumerable<string> Transports { get; set; }

        public bool ShouldSerializeTransports() => Transports != null;

        public string ProtocolVersion { get; set; }

        public double TransportConnectTimeout { get; set; }

        public double LongPollDelay { get; set; }
    }
}
