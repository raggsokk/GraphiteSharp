﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;


namespace GraphiteSharp
{
    public class UdpCarbonClient : CarbonClient
    {
        public UdpCarbonClient(IPEndPoint Endpoint, string MetricPrefix = null)
            : base(Endpoint, MetricPrefix)
        { }

        public UdpCarbonClient(string IpOrHostName, int port = 2003, string MetricPrefix = null)
            : base(CarbonClient.CreateIpEndpoint(IpOrHostName, port), MetricPrefix)
        { }

        protected override void Send(List<byte[]> payloads)
        {
            if (pSocket == null)
                pSocket = new Socket(Endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            if (!pSocket.Connected)
                pSocket.Connect(Endpoint);

            //TODO: Batch up to 512 (ipv6: 1500) bytes which should be minimum safe batch number.
            foreach (var msg in payloads)
            {
                pSocket.Send(msg);
            }

        }

        protected override async Task SendAsync(List<byte[]> payloads)
        {
            //await Task.Run(() => { Send(payloads); });
            throw new NotImplementedException();

            //if (pSocket == null)
            //    pSocket = new Socket(Endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);


            //throw new NotImplementedException();
        }

    }
}
