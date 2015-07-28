#region License
//
// UdpCarbonClient.cs
// 
// The MIT License (MIT)
//
// Copyright (c) 2015 Jarle Hansen
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE. 
//
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;


namespace GraphiteSharp
{
    /// <summary>
    /// A CarbonClient implementation using UDP Protocol for communication with a Graphite Carbon Backend.
    /// </summary>
    public class UdpCarbonClient : CarbonClient
    {
        /// <summary>
        /// Creates a new UdpCarbonClient with specific IPEndPoint.
        /// </summary>
        /// <param name="Endpoint">Already created IPEndPoint pointing to Carbon backend.</param>
        /// <param name="MetricPrefix">Optionally set a default MetricPrefix for all send operations.</param>
        /// <param name="options">Option object with possible alternative behaviour.</param>
        public UdpCarbonClient(IPEndPoint Endpoint, string MetricPrefix = null, CarbonClientOptions options = null)
            : base(Endpoint, MetricPrefix, options)
        { }

        /// <summary>
        /// Creates a new UdpCarbonClient with IpOrHostname and default port 2003.
        /// </summary>
        /// <param name="IpOrHostname">Ip or Hostname of host where carbon backend is.</param>
        /// <param name="port">Optionally override carbon backed port listens on.</param>
        /// <param name="MetricPrefix">Optionally set a default MetricPrefix for all send operations.</param>
        /// <param name="options">Option object with possible alternative behaviour.</param>
        public UdpCarbonClient(string IpOrHostname, int port = 2003, string MetricPrefix = null, CarbonClientOptions options = null)
            : base(CarbonClient.CreateIpEndpoint(IpOrHostname, port), MetricPrefix, options)
        { }

        /// <summary>
        /// The actuall function responsible for sending Payloads with UDP to a Carbon Backend.
        /// </summary>
        /// <param name="payloads"></param>
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

        /// <summary>
        /// The actuall async function responsible for sending Payloads with UDP to a Carbon Backend.
        /// </summary>
        /// <param name="payloads"></param>
        /// <returns></returns>
        protected override async Task SendAsync(List<byte[]> payloads)
        {
            if (pSocket == null)
            {
                pSocket = new Socket(Endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                //pSocket.SendToAsync()
                pSocket.Connect(Endpoint); //UDP doesnt connect. Only sets remote end point.
            }

#if !NOFANCYASYNC

            var args = new SocketAsyncEventArgs();
            //args.RemoteEndPoint = Endpoint;
            var awaitable = new SocketAwaitable(args);

            // udp with connect??
            //await pSocket.ConnectAsync(awaitable);

            foreach(var msg in payloads)
            {
                args.SetBuffer(msg, 0, msg.Length);

                await pSocket.SendAsync(awaitable);
            }

#else

            foreach(var msg in payloads)
            {
                var result = pSocket.BeginSend(msg, 0, msg.Length, SocketFlags.None, null, pSocket);
                await Task.Factory.FromAsync(result, pSocket.EndSend);
            }

#endif
        }

    }
}
