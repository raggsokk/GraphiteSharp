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
        public TcpCarbonClient(IPEndPoint endpoint) : base(endpoint, null)
        { }

        public TcpCarbonClient(string IpOrHostname, int Port = 2003, string MetricPrefix = null)
            : base(CarbonClient.CreateIpEndpoint(IpOrHostname, Port), MetricPrefix)
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
            //await Task.Run(() => { Send(payloads); });
            throw new NotImplementedException();
        }

    }
}
