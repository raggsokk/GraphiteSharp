using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;

using System.Reflection;

namespace GraphiteSharp
{
    public abstract class CarbonClient : IDisposable
    {
        public IPEndPoint Endpoint { get; protected set; }
        public string MetricPrefix { get; set; }

        protected Socket pSocket;

        protected CarbonClient(IPEndPoint endPoint, string metrixPrefix)
        {
            this.Endpoint = endPoint;
            this.MetricPrefix = metrixPrefix;
        }

        #region Public Api

        public virtual void Send(string MetricName, object value, DateTime? timestamp = null)
        {
            var payloads = GeneratePayloads(MetricName, value, timestamp);

            Send(payloads);
        }

        public virtual async Task SendAsync(string MetricName, object value, DateTime? timestamp = null)
        {
            var payloads = GeneratePayloads(MetricName, value, timestamp);

            await SendAsync(payloads);
        }

        #endregion

        #region Worker Functions handling actuall calls

        protected virtual void Send(List<byte[]> payloads)
        {
            SendAsync(payloads).Wait();
        }

        protected abstract Task SendAsync(List<byte[]> payloads);

        #endregion

        #region Utility functions

        protected virtual List<byte[]> GeneratePayloads(string MetricName, object value, DateTime? timestamp = null)
        {
            if (timestamp == null || !timestamp.HasValue)
                timestamp = DateTime.Now;

            var list = new List<byte[]>();

            var t = value.GetType();

            if(t.IsPrimitive) // assumes single value type.
            {
                list.Add(GeneratePayload(MetricName, value, timestamp.Value));
            }
            else
            {
                // fancy anon type thingy.
                // currently uses reflection directly which is very slow.
                // it doesn't cache this data per call so.. very slow.
                var reuse = new StringBuilder();
                var tinfo = t.GetTypeInfo();

                var props = tinfo.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                foreach (var p in props)
                {
                    if (!p.CanRead)
                        continue;
                    if (p.GetIndexParameters()?.Length > 0)
                        continue; // disable indexers.

                    //list.Add(GeneratePayload(string.Join(".", MetricName, p.Name), p.GetValue(null), timestamp, reuse));
                    list.Add(GeneratePayload(p.Name, p.GetValue(null), timestamp.Value, reuse, MetricName));
                }

                //var fields = tinfo.GetRuntimeFields();
                var fields = tinfo.GetFields(BindingFlags.Instance | BindingFlags.Public);
                foreach (var f in fields)
                {
                    //if(!f.)
                    list.Add(GeneratePayload(f.Name, f.GetValue(null), timestamp.Value, reuse, MetricName));
                    //list.Add(GeneratePayload(string.Join(".", MetricName, f.Name), f.GetValue(null), timestamp, reuse));
                }

            }


            return list;
        }

        protected virtual byte[] GeneratePayload(string MetricName, object value, DateTime timestamp, StringBuilder reuse = null, string AdditionalPrefix = null)
        {
            if (reuse == null)
                reuse = new StringBuilder();
            else
                reuse.Length = 0;

            if (!string.IsNullOrWhiteSpace(this.MetricPrefix))
            {
                reuse.Append(this.MetricPrefix);
                reuse.Append(".");
            }

            if (!string.IsNullOrWhiteSpace(AdditionalPrefix))
            {
                reuse.Append(AdditionalPrefix);
                reuse.Append(".");
            }

            reuse.Append(MetricName);
            reuse.AppendFormat(" {0} ", value);
            reuse.Append(DateTimeToUnixEpoch(timestamp));
            reuse.Append(" \n");

            //var bytes = ASCIIEncoding.ASCII.GetBytes(reuse.ToString());
            var bytes = UTF8Encoding.UTF8.GetBytes(reuse.ToString());

            return bytes;
        }

        private static readonly DateTime UNIX1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        protected virtual int DateTimeToUnixEpoch(DateTime dt)
        {
            //var t = (dt.ToUniversalTime() - UNIX1970);
            //return (int)t.TotalSeconds;
            //return (int)(dt.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            return (int)(dt.ToUniversalTime().Subtract(UNIX1970)).TotalSeconds;
        }

        protected static IPEndPoint CreateIpEndpoint(string IpOrHostname, int port = 2003)
        {
            return new IPEndPoint(GetIpAddress(IpOrHostname), port);
        }

        protected static IPAddress GetIpAddress(string IpOrHostname)
        {
            IPAddress address = null;

            if(!IPAddress.TryParse(IpOrHostname, out address))
            {
                //TODO: not async.
                address = Dns.GetHostEntry(IpOrHostname)?.AddressList.First();
            }

            return address;
        }

        #endregion

        #region Properties

        public IPAddress IpAddress
        {
            get
            {
                return Endpoint.Address;
            }
        }

        public AddressFamily AddressFamily
        {
            get
            {
                return Endpoint.AddressFamily;
            }
        }

        public int Port
        {
            get
            {
                return Endpoint.Port;
            }
        }

        #endregion

        #region IDisposable Implementation

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool Disposing)
        {
            if(Disposing)
            {
                if (pSocket != null && pSocket.Connected && pSocket.SocketType != SocketType.Dgram)
                    pSocket.Disconnect(true);

            }

            pSocket?.Dispose();
        }

        ~CarbonClient()
        {
            this.Dispose(false);
        }

        #endregion
    }
}
