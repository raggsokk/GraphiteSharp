#region License
//
// TcpCarbonClient.cs
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
    /// A CarbonClient implementation using TCP Protocol for communication with a Graphite Carbon Backend.
    /// </summary>
    public class TcpCarbonClient : CarbonClient
    {
        /// <summary>
        /// Creates a new TcpCarbonClient with specific IPEndPoint.
        /// </summary>
        /// <param name="endpoint">Already created IPEndPoint pointing to Carbon backend.</param>
        /// <param name="MetricPrefix">Optionally set a default MetricPrefix for all send operations.</param>
        /// <param name="options">Option object with possible alternative behaviour.</param>
        public TcpCarbonClient(IPEndPoint endpoint, string MetricPrefix = null, CarbonClientOptions options = null) 
            : base(endpoint, MetricPrefix, options)
        { }

        /// <summary>
        /// Creates a new TcpCarbonClient with IpOrHostname and default port 2003.
        /// </summary>
        /// <param name="IpOrHostname">Ip or Hostname of host where carbon backend is.</param>
        /// <param name="Port">Optionally override carbon backed port listens on.</param>
        /// <param name="MetricPrefix">Optionally set a default MetricPrefix for all send operations.</param>
        /// <param name="options">Option object with possible alternative behaviour.</param>
        public TcpCarbonClient(string IpOrHostname, int Port = 2003, string MetricPrefix = null, CarbonClientOptions options = null)
            : base(CarbonClient.CreateIpEndpoint(IpOrHostname, Port), MetricPrefix, options)
        { }

        /// <summary>
        /// The actuall function responsible for sending Payloads with TCP to a Carbon Backend.
        /// </summary>
        /// <param name="payloads"></param>
        protected override void Send(List<byte[]> payloads)
        {
            if (pSocket == null)
                pSocket = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.IP);

            if (!pSocket.Connected)
                pSocket.Connect(Endpoint);

            //TODO: Batch up to 512 (ipv6: 1500) bytes which should be minimum safe batch number.
            foreach (var msg in payloads)
            {
                pSocket.Send(msg);
            }

        }

        /// <summary>
        /// The actuall async function responsible for sending Payloads with TCP to a Carbon Backend.
        /// </summary>
        /// <param name="payloads"></param>
        /// <returns></returns>
        protected override async Task SendAsync(List<byte[]> payloads)
        {
            if (pSocket == null)
                pSocket = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.IP);

#if !NOFANCYASYNC
            var args = new SocketAsyncEventArgs();
            var awaitable = new SocketAwaitable(args);

            if (!pSocket.Connected)
            {
                //pSocket.Connect(Endpoint);
                args.RemoteEndPoint = Endpoint;

                await pSocket.ConnectAsync(awaitable);
            }

            foreach(var msg in payloads)
            {
                args.SetBuffer(msg, 0, msg.Length);
                await pSocket.SendAsync(awaitable);
            }
#else

            if (!pSocket.Connected)
            {
                //await Task.
                await Task.Factory.FromAsync(
                    pSocket.BeginConnect,
                    pSocket.EndConnect,
                    Endpoint, null);
            }

            foreach(var msg in payloads)
            {
                // no fromasync accepts 4 args.
                var result = pSocket.BeginSend(msg, 0, msg.Length, SocketFlags.None, null, pSocket);

                await Task.Factory.FromAsync(result, pSocket.EndSend);
            }

#endif

        }

    }
}
