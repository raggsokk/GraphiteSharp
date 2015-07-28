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
    internal sealed class SocketAwaitable : INotifyCompletion
    {
        /// <summary>
        /// Dummy no action action.
        /// </summary>
        private readonly static Action SENTINEL = () => { };

        /// <summary>
        /// Was this task already completed?
        /// </summary>
        internal bool pWasCompleted;
        /// <summary>
        /// Action to do when we are completed.
        /// </summary>
        internal Action pContinuation;
        /// <summary>
        /// reference to SocketAsyncEventArgs we are wrapping.
        /// </summary>
        internal SocketAsyncEventArgs pEventArgs;

        /// <summary>
        /// Wraps a SocketAsyncEventArgs into awaitable interface.
        /// </summary>
        /// <param name="eventArgs"></param>
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

        /// <summary>
        /// Resets object for reuse.
        /// </summary>
        public void Reset()
        {
            pWasCompleted = false;
            pContinuation = null;
        }

        public SocketAwaitable GetAwaiter() { return this; }

        /// <summary>
        /// Is this awaitable completed?
        /// </summary>
        public bool IsCompleted { get { return pWasCompleted; } }

        /// <summary>
        /// Func called after action is completed.
        /// </summary>
        /// <param name="continuation"></param>
        public void OnCompleted(Action continuation)
        {
            if(pContinuation == SENTINEL ||
                Interlocked.CompareExchange(
                    ref pContinuation, continuation, null) == SENTINEL)
            {
                Task.Run(continuation);
            }
        }

        /// <summary>
        /// Return the result from this await task.
        /// </summary>
        public void GetResult()
        {
            if (pEventArgs.SocketError != SocketError.Success)
                throw new SocketException((int)pEventArgs.SocketError);
        }

        //public bool I

    }
}
#endif