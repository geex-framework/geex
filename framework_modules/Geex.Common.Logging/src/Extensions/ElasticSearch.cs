using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using GeexBox.ElasticSearch.Zero.Logging.Elasticsearch;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;


// ReSharper disable once CheckNamespace
namespace ElasticSearch
{
    public static class ElasticSearchExtensions
    {
        public static IPAddress GetLocalIPv4(this IEnumerable<IPAddress> addressList)
        {
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == NetworkInterfaceType.Ethernet && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address;
                        }
                    }
                }
            }
            return null;
        }
    }
}
