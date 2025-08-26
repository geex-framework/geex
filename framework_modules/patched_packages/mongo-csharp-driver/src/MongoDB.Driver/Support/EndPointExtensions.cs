/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace MongoDB.Driver.Support
{
    internal static class EndPointExtensions
    {
        public static IEnumerable<MongoServerAddress> ToMongoServerAddresses(this IEnumerable<EndPoint> endPoints)
        {
            return endPoints.Select(endPoint =>
            {
                DnsEndPoint dnsEndPoint;
                IPEndPoint ipEndPoint;
                if ((dnsEndPoint = endPoint as DnsEndPoint) != null)
                {
                    return new MongoServerAddress(dnsEndPoint.Host, dnsEndPoint.Port);
                }
                else if ((ipEndPoint = endPoint as IPEndPoint) != null)
                {
                    var address = ipEndPoint.Address.ToString();
                    if (ipEndPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        address = "[" + address + "]";
                    }
                    return new MongoServerAddress(address, ipEndPoint.Port);
                }
                else
                {
                    throw new NotSupportedException("Only DnsEndPoint and IPEndPoints are supported in the connection string.");
                }
            });
        }
    }
}
