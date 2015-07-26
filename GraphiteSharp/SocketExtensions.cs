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
    public static class SocketExtensions
    {
        internal static SocketAwaitable ConnectAsync(this Socket socket,
            SocketAwaitable awaitable)
        {
            awaitable.Reset();

            if (!socket.ConnectAsync(awaitable.pEventArgs))
                awaitable.pWasCompleted = true;

            return awaitable;
        }

        internal static SocketAwaitable SendAsync(this Socket socket, 
            SocketAwaitable awaitable)
        {
            awaitable.Reset();

            if (!socket.SendAsync(awaitable.pEventArgs))
                awaitable.pWasCompleted = true;

            return awaitable;
        }

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