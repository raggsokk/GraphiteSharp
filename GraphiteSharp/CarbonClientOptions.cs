#region License
//
// CarbonClientOptions.cs
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

namespace GraphiteSharp
{
    /// <summary>
    /// An option class with optionale behavours for the CarbonClient.
    /// </summary>
    //TODO: rename this to just CarbonOptions?
    public class CarbonClientOptions
    {
        /// <summary>
        /// Convert timestamps to utc before transmit to a graphite server?
        /// </summary>
        public bool ConvertToUtc { get; set; }

        /// <summary>
        /// Sanitize Metric Names to valid names only?
        /// </summary>
        public bool SanitizeMetricNames { get; set; }

        /// <summary>
        /// If Sanitize is enabled, convert text to lower case?
        /// </summary>
        public bool SanitizeToLowerCase { get; set; }

        /// <summary>
        /// Default options used when not otherwise specified.
        /// </summary>
        public static readonly CarbonClientOptions DefaultOptions = new CarbonClientOptions()
        {
            ConvertToUtc = true,
            SanitizeMetricNames = true,
            SanitizeToLowerCase = true,
        };
    }
}
