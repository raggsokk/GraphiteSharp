using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;


namespace GraphiteSharp
{
    public class TcpCarbonClient : CarbonClient
    {
        public TcpCarbonClient(IPEndPoint endpoint, string MetricPrefix = null, CarbonClientOptions options = null) 
            : base(endpoint, MetricPrefix, options)
        { }

        public TcpCarbonClient(string IpOrHostname, int Port = 2003, string MetricPrefix = null, CarbonClientOptions options = null)
            : base(CarbonClient.CreateIpEndpoint(IpOrHostname, Port), MetricPrefix, options)
        { }

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
