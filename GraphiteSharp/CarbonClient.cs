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

        public CarbonClientOptions Options { get; protected set; }

        protected Socket pSocket;

        protected CarbonClient(IPEndPoint endPoint, string metrixPrefix, CarbonClientOptions options = null)
        {
            this.Endpoint = endPoint;
            //this.MetricPrefix = metrixPrefix;
            this.MetricPrefix = SanitizeMetricName(metrixPrefix);

            if (options == null)
                this.Options = CarbonClientOptions.DefaultOptions;
            else
                this.Options = options;
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

            //TODO: Handle invariant floating point conversion better. Aka Graphite requires "#.#" format, but default norwegian converson is "#,#". Handle this.
            var v = value.ToString();
            if (v.Contains(",")) // bad hack conversion.
                v.Replace(',', '.');
            reuse.AppendFormat(" {0} ", v);

            reuse.Append(DateTimeToUnixEpoch(timestamp));
            reuse.Append(" \n");

            //TODO: prevent sanatize already sanatized prefix.
            var bytes = ASCIIEncoding.ASCII.GetBytes(SanitizeMetricName(reuse.ToString()));
            //var bytes = ASCIIEncoding.ASCII.GetBytes(reuse.ToString());
            //var bytes = UTF8Encoding.UTF8.GetBytes(reuse.ToString());

            return bytes;
        }

        private static readonly DateTime UNIX1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Sanitizes Metric names obeing CarbonClientOptions.
        /// </summary>
        /// <param name="MetricName"></param>
        /// <param name="reuse"></param>
        /// <returns></returns>
        protected virtual string SanitizeMetricName(string MetricName, StringBuilder reuse = null)            
        {
            if (!Options.SanitizeMetricNames)
                return MetricName;

            if (reuse == null)
                reuse = new StringBuilder();
            else
                reuse.Length = 0;

            var charArray = MetricName.ToCharArray();

            bool flagLowerCase = Options.SanitizeToLowerCase;

            foreach(var c in charArray)
            {
                switch(c)
                {
                    case '\\':
                    case '/':
                        reuse.Append('.');
                        break;
                    case ' ':
                    case '_':
                        reuse.Append('_');
                        break;
                    default:
                        if (flagLowerCase && char.IsLetter(c))
                            reuse.Append(char.ToLowerInvariant(c));
                        else
                            reuse.Append(c);

                        break;
                }
            }

            return reuse.ToString();
        }

        /// <summary>
        /// Converts DateTime to Unix Epic time.
        /// It respects CarbonClientOptions.ConvertToUtc
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        protected virtual long DateTimeToUnixEpoch(DateTime dt)
        {
            if (this.Options.ConvertToUtc)
                dt = dt.ToUniversalTime();

            return (long)(dt.Subtract(UNIX1970)).TotalSeconds;
            //return (int)(dt.ToUniversalTime().Subtract(UNIX1970)).TotalSeconds;
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
