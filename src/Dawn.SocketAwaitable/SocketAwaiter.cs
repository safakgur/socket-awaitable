// Copyright
// ----------------------------------------------------------------------------------------------------------
//  <copyright file="SocketAwaiter.cs" company="https://github.com/safakgur/Dawn.SocketAwaitable">
//      MIT
//  </copyright>
//  <license>
//      This source code is subject to terms and conditions of The MIT License (MIT).
//      A copy of the license can be found in the License.txt file at the root of this distribution.
//  </license>
//  <summary>
//      Provides an class for awaiting asynchronous socket arguments.
//  </summary>
// ----------------------------------------------------------------------------------------------------------

namespace Dawn.Net.Sockets
{
    using System;
    using System.Diagnostics;
    using System.Net.Sockets;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    ///     Provides an object that waits for the completion of a <see cref="SocketAwaitable" />.
    ///     This class is not thread-safe, therefore it doesn't support multiple concurrent awaiters.
    /// </summary>
    [DebuggerDisplay("Completed: {IsCompleted}")]
    public sealed class SocketAwaiter : INotifyCompletion
    {
        #region Fields
        /// <summary>
        ///     A sentinel delegate that does nothing.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly Action sentinel = delegate { };

        /// <summary>
        ///     The asynchronous socket arguments to await.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly SocketAwaitable awaitable;

        /// <summary>
        ///     An object to synchronize access to the awaiter for validations.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly object syncRoot = new object();

        /// <summary>
        ///     The continuation delegate that will be called after the current operation is awaited.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Action continuation;

        /// <summary>
        ///     A value indicating whether the asynchronous operation is completed.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private bool isCompleted = true;
        #endregion

        #region Constructors
        /// <summary>
        ///     Initializes a new instance of the <see cref="SocketAwaiter" /> class.
        /// </summary>
        /// <param name="awaitable">
        ///     The asynchronous socket arguments to await.
        /// </param>
        internal SocketAwaiter(SocketAwaitable awaitable)
        {
            this.awaitable = awaitable;
            this.awaitable.Arguments.Completed += delegate
            {
                lock (this.SyncRoot)
                    this.IsCompleted = true;

                var c = this.continuation ?? Interlocked.CompareExchange(ref this.continuation, sentinel, null);
                if (c != null)
                    c.Invoke();
            };
        }
        #endregion

        #region Properties
        /// <summary>
        ///     Gets a value indicating whether the asynchronous operation is completed.
        /// </summary>
        public bool IsCompleted
        {
            get { return this.isCompleted; }
            internal set { this.isCompleted = value; }
        }

        /// <summary>
        ///     Gets an object to synchronize access to the awaiter for validations.
        /// </summary>
        internal object SyncRoot
        {
            get { return this.syncRoot; }
        }
        #endregion

        #region Methods
        /// <summary>
        ///     Gets the result of the asynchronous socket operation.
        /// </summary>
        /// <returns>
        ///     A <see cref="SocketError" /> that represents the result of the socket operations.
        /// </returns>
        public SocketError GetResult()
        {
            return this.awaitable.Arguments.SocketError;
        }

        /// <summary>
        ///     Gets invoked when the asynchronous operation is completed and runs the specified delegate as
        ///     continuation.
        /// </summary>
        /// <param name="continuation">
        ///     Continuation to run.
        /// </param>
        void INotifyCompletion.OnCompleted(Action continuation)
        {
            if (this.continuation == sentinel
                || Interlocked.CompareExchange(ref this.continuation, continuation, null) == sentinel)
                Task.Run(continuation);
        }

        /// <summary>
        ///     Resets this awaiter for re-use.
        /// </summary>
        internal void Reset()
        {
            this.awaitable.Arguments.AcceptSocket = null;
            this.awaitable.Arguments.SocketError = SocketError.AlreadyInProgress;
            this.IsCompleted = false;
            this.continuation = null;
        }
        #endregion
    }
}