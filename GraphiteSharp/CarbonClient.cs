#region License
//
// CarbonClient.cs
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

using System.Reflection;

namespace GraphiteSharp
{
    /// <summary>
    /// Abstract class with common functionality for sending data to a Graphite Carbon Backend.
    /// </summary>
    public abstract class CarbonClient : IDisposable
    {
        /// <summary>
        /// This is the ip endpoint (ipaddress:port) where Carbon server listens.
        /// </summary>
        public IPEndPoint Endpoint { get; protected set; }
        /// <summary>
        /// Gets or Sets a common prefix for all Metric Names used during send to carbon backend.
        /// </summary>
        public string MetricPrefix { get; set; }

        /// <summary>
        /// Options for alternative CarbonClient behaviour.
        /// Changing values after CarbonClient creation is undefined.
        /// </summary>
        public CarbonClientOptions Options { get; protected set; }

        /// <summary>
        /// Protected socket for implementators.
        /// </summary>
        protected Socket pSocket;

        /// <summary>
        /// Base Constructor which sets the CarbonClient in known state.
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="metrixPrefix"></param>
        /// <param name="options"></param>
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

        /// <summary>
        /// Send data based on which protocol is used. (tcp/udp).
        /// </summary>
        /// <param name="MetricName">The name to store values as. Optinally prefixed.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="timestamp">Optional the timestamp to write. Default is now.</param>
        public virtual void Send(string MetricName, object value, DateTime? timestamp = null)
        {
            var payloads = GeneratePayloads(MetricName, value, timestamp);

            Send(payloads);
        }

        /// <summary>
        /// Sends data async based on which protocol class was created.
        /// </summary>
        /// <param name="MetricName">The name to store values as. Optinally prefixed.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="timestamp">Optional the timestamp to write. Default is now.</param>
        /// <returns></returns>
        public virtual async Task SendAsync(string MetricName, object value, DateTime? timestamp = null)
        {
            var payloads = GeneratePayloads(MetricName, value, timestamp);

            await SendAsync(payloads);
        }

        #endregion

        #region Worker Functions handling actuall calls

        /// <summary>
        /// The actuall worker func implementations has to implent.
        /// Default uses async path with wait, so please override!
        /// </summary>
        /// <param name="payloads"></param>
        protected virtual void Send(List<byte[]> payloads)
        {
            SendAsync(payloads).Wait();
        }

        /// <summary>
        /// The actuall async worker func implementations has to implent.
        /// </summary>
        /// <param name="payloads"></param>
        /// <returns></returns>
        protected abstract Task SendAsync(List<byte[]> payloads);

        #endregion

        #region Utility functions

        /// <summary>
        /// Generates the byte[] payloads to send to carbon.
        /// </summary>
        /// <param name="MetricName"></param>
        /// <param name="value"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Generates a single byte[] payload to send to carbon.
        /// </summary>
        /// <param name="MetricName"></param>
        /// <param name="value"></param>
        /// <param name="timestamp"></param>
        /// <param name="reuse"></param>
        /// <param name="AdditionalPrefix"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Used for calculating Unix Epoch int32 timestamps.
        /// Would have been constant if possible.
        /// </summary>
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

        /// <summary>
        /// Responsible for converting ip or hostname into a valid IPEndPoint class.
        /// </summary>
        /// <param name="IpOrHostname"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        protected static IPEndPoint CreateIpEndpoint(string IpOrHostname, int port = 2003)
        {
            return new IPEndPoint(GetIpAddress(IpOrHostname), port);
        }

        /// <summary>
        /// Responsible for converting ip or hostname into a valid IPAddress class.
        /// </summary>
        /// <param name="IpOrHostname"></param>
        /// <returns></returns>
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

        /// <summary>
        /// The IpAddress to send carbon data to.
        /// </summary>
        public IPAddress IpAddress
        {
            get
            {
                return Endpoint.Address;
            }
        }

        /// <summary>
        /// Protocol to use when sending carbon data.
        /// </summary>
        public AddressFamily AddressFamily
        {
            get
            {
                return Endpoint.AddressFamily;
            }
        }

        /// <summary>
        /// The port to contact carbon backend at.
        /// </summary>
        public int Port
        {
            get
            {
                return Endpoint.Port;
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Dispose this object.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// The actuall disposing function.
        /// </summary>
        /// <param name="Disposing">Are we explicit disposing?</param>
        protected virtual void Dispose(bool Disposing)
        {
            if(Disposing)
            {
                if (pSocket != null && pSocket.Connected && pSocket.SocketType != SocketType.Dgram)
                    pSocket.Disconnect(true);

            }

            pSocket?.Dispose();
        }

        /// <summary>
        /// Implicit disposing.
        /// </summary>
        ~CarbonClient()
        {
            this.Dispose(false);
        }

        #endregion
    }
}
