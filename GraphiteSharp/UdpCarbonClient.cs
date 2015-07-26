using System;
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
        public UdpCarbonClient(IPEndPoint Endpoint, string MetricPrefix = null, CarbonClientOptions options = null)
            : base(Endpoint, MetricPrefix, options)
        { }

        public UdpCarbonClient(string IpOrHostname, int port = 2003, string MetricPrefix = null, CarbonClientOptions options = null)
            : base(CarbonClient.CreateIpEndpoint(IpOrHostname, port), MetricPrefix, options)
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
