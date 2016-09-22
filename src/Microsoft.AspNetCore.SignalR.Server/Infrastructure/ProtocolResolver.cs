// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.SignalR.Infrastructure
{
    public class ProtocolResolver
    {
        private readonly Version _minSupportedProtocol;
        private readonly Version _maxSupportedProtocol;
        public static readonly Version ProtocolVersion_1_4 = new Version(1, 4);
        public static readonly Version ProtocolVersion_1_6 = new Version(1, 6);

        public ProtocolResolver() :
            this(new Version(1, 2), new Version(1, 6))
        {
        }

        public ProtocolResolver(Version min, Version max)
        {
            _minSupportedProtocol = min;
            _maxSupportedProtocol = max;
        }

        public Version Resolve(string clientProtocol)
        {
            Version clientProtocolVersion;

            if (Version.TryParse(clientProtocol, out clientProtocolVersion))
            {
                if (clientProtocolVersion > _maxSupportedProtocol)
                {
                    clientProtocolVersion = _maxSupportedProtocol;
                }
                else if (clientProtocolVersion < _minSupportedProtocol)
                {
                    clientProtocolVersion = _minSupportedProtocol;
                }
            }

            return clientProtocolVersion ?? _minSupportedProtocol;
        }

        public bool IsClientProtocolEqualOrNewer(string clientProtocol, Version version)
        {
            return Resolve(clientProtocol) >= version;
        }
    }
}
