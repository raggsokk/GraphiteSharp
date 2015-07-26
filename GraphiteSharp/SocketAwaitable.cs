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
    /// 
    /// Based on code from http://blogs.msdn.com/b/pfxteam/archive/2011/12/15/10248293.aspx
    /// </summary>
    internal sealed class SocketAwaitable : INotifyCompletion
    {
        private readonly static Action SENTINEL = () => { };

        internal bool pWasCompleted;
        internal Action pContinuation;
        internal SocketAsyncEventArgs pEventArgs;

        public SocketAwaitable(SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs == null)
                throw new ArgumentNullException(nameof(eventArgs));

            this.pEventArgs = eventArgs;

            eventArgs.Completed += delegate
            {
                // not actually sure what happens here...
                var prev = pContinuation ?? Interlocked.CompareExchange(
                    ref pContinuation, SENTINEL, null);

                if (prev != null) prev();
            };
        }

        public void Reset()
        {
            pWasCompleted = false;
            pContinuation = null;
        }

        public SocketAwaitable GetAwaiter() { return this; }

        public bool IsCompleted { get { return pWasCompleted; } }

        public void OnCompleted(Action continuation)
        {
            if(pContinuation == SENTINEL ||
                Interlocked.CompareExchange(
                    ref pContinuation, continuation, null) == SENTINEL)
            {
                Task.Run(continuation);
            }
        }

        public void GetResult()
        {
            if (pEventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)pEventArgs.SocketError);
        }

        //public bool I

    }
}
#endif