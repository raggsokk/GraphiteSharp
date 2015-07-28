#region License
//
// SocketAwaitable.cs
// 
// The MIT License (MIT)
//
// Based on code from http://blogs.msdn.com/b/pfxteam/archive/2011/12/15/10248293.aspx
// So Copyright (c) 2011 Stephen Toub
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

#if !NOFANCYASYNC
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.CompilerServices;

namespace GraphiteSharp
{
    /// <summary>
    /// Based on code from http://blogs.msdn.com/b/pfxteam/archive/2011/12/15/10248293.aspx
    /// </summary>
    //TODO: Add Async disconnect?
    public static class SocketExtensions
    {
        /// <summary>
        /// Creates a proper awaitable async connect object.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="awaitable"></param>
        /// <returns></returns>
        internal static SocketAwaitable ConnectAsync(this Socket socket,
            SocketAwaitable awaitable)
        {
            awaitable.Reset();

            if (!socket.ConnectAsync(awaitable.pEventArgs))
                awaitable.pWasCompleted = true;

            return awaitable;
        }

        /// <summary>
        /// Creates a proper awaitable async send object.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="awaitable"></param>
        /// <returns></returns>
        internal static SocketAwaitable SendAsync(this Socket socket, 
            SocketAwaitable awaitable)
        {
            awaitable.Reset();

            if (!socket.SendAsync(awaitable.pEventArgs))
                awaitable.pWasCompleted = true;

            return awaitable;
        }

        /// <summary>
        /// Creates a proper awaitable async receive object.
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="awaitable"></param>
        /// <returns></returns>
        internal static SocketAwaitable ReceiveAsync(this Socket socket,
            SocketAwaitable awaitable)
        {
            awaitable.Reset();

            if (!socket.ReceiveAsync(awaitable.pEventArgs))
                awaitable.pWasCompleted = true;

            return awaitable;
        }
    }
}
#endif