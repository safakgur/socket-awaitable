// Copyright
// ----------------------------------------------------------------------------------------------------------
//  <copyright file="SocketExTests.cs" company="https://github.com/safakgur/Dawn.SocketAwaitable">
//      MIT
//  </copyright>
//  <license>
//      This source code is subject to terms and conditions of The MIT License (MIT).
//      A copy of the license can be found in the License.txt file at the root of this distribution.
//  </license>
//  <summary>
//      Provides a class that contains unit tests for awaitable socket extensions.
//  </summary>
// ----------------------------------------------------------------------------------------------------------

namespace Dawn.Net.Sockets.Tests
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     Contains unit tests for <see cref="SocketEx" /> class.
    /// </summary>
    [TestClass]
    public class SocketExTests
    {
        #region Methods
        /// <summary>
        ///     Tests <see cref="SocketEx.AcceptAsync" />, <see cref="SocketEx.ConnectAsync" />,
        ///     <see cref="SocketEx.ReceiveAsync" /> and <see cref="SocketEx.SendAsync" /> methods.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> that represents the asynchronous testing operation.
        /// </returns>
        [TestMethod]
        public async Task TestCommonOperations()
        {
            // Listen.
            using (var listener = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                listener.Listen(1);
                var acceptReceiveTask = Task.Run(async () =>
                {
                    // Accept.
                    Socket accepted;
                    using (var acceptAwaitable = new SocketAwaitable())
                    {
                        Assert.IsNull(acceptAwaitable.AcceptSocket);

                        var acceptResult = await listener.AcceptAsync(acceptAwaitable);
                        Assert.AreEqual(acceptResult, SocketError.Success);
                        Assert.IsNotNull(acceptAwaitable.AcceptSocket);
                        accepted = acceptAwaitable.AcceptSocket;
                    }

                    // Receive.
                    using (var receiveAwaitable = new SocketAwaitable())
                    {
                        receiveAwaitable.Buffer = new ArraySegment<byte>(new byte[16], 2, 14);

                        var receiveResult = await accepted.ReceiveAsync(receiveAwaitable);
                        Assert.AreEqual(receiveResult, SocketError.Success);
                        Assert.AreEqual(receiveAwaitable.Transferred.Count, 1);
                        Assert.AreEqual(receiveAwaitable.Buffer.Array[receiveAwaitable.Buffer.Offset], 7);
                    }
                });

                // Connect.
                using (var client = new Socket(SocketType.Stream, ProtocolType.Tcp))
                {
                    using (var connectAwaitable = new SocketAwaitable())
                    {
                        connectAwaitable.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, (listener.LocalEndPoint as IPEndPoint).Port);

                        var connectResult = await client.ConnectAsync(connectAwaitable);
                        Assert.AreEqual(connectResult, SocketError.Success);
                    }

                    await Task.Delay(500);

                    // Send.
                    using (var sendAwaitable = new SocketAwaitable())
                    {
                        sendAwaitable.Buffer = new ArraySegment<byte>(new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 }, 7, 1);

                        var sendResult = await client.SendAsync(sendAwaitable);
                        Assert.AreEqual(sendResult, SocketError.Success);
                    }

                    // Await accept/receive task.
                    await acceptReceiveTask;
                    Assert.AreEqual(acceptReceiveTask.Status, TaskStatus.RanToCompletion);
                }
            }
        }

        /// <summary>
        ///     Tests the awaiters before and after calling <see cref="SocketEx" /> methods.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> that represents the asynchronous testing operation.
        /// </returns>
        [TestMethod]
        public async Task TestAwaiterStatus()
        {
            using (var listener = new Socket(SocketType.Stream, ProtocolType.Tcp))
            {
                listener.Bind(new IPEndPoint(IPAddress.IPv6Any, 0));
                listener.Listen(1);
                var acceptTask = Task.Run(async () =>
                {
                    using (var awaitable = new SocketAwaitable())
                    {
                        var awaiter = awaitable.GetAwaiter();
                        Assert.IsTrue(awaiter.IsCompleted);
                        Assert.AreEqual(awaiter.GetResult(), SocketError.Success);

                        var a = listener.AcceptAsync(awaitable);
                        Assert.IsFalse(awaiter.IsCompleted);
                        Assert.AreEqual(awaiter.GetResult(), SocketError.AlreadyInProgress);

                        var result = await a;
                        Assert.IsTrue(awaiter.IsCompleted);
                        Assert.AreEqual(awaiter.GetResult(), result);
                    }
                });

                await Task.Delay(500);

                using (var client = new Socket(SocketType.Stream, ProtocolType.Tcp))
                {
                    using (var awaitable = new SocketAwaitable())
                    {
                        awaitable.RemoteEndPoint = new IPEndPoint(IPAddress.IPv6Loopback, (listener.LocalEndPoint as IPEndPoint).Port);

                        var awaiter = awaitable.GetAwaiter();
                        Assert.IsTrue(awaiter.IsCompleted);
                        Assert.AreEqual(awaiter.GetResult(), SocketError.Success);

                        var a = client.ConnectAsync(awaitable);
                        Assert.IsFalse(awaiter.IsCompleted);
                        Assert.AreEqual(awaiter.GetResult(), SocketError.AlreadyInProgress);

                        var result = await a;
                        Assert.IsTrue(awaiter.IsCompleted);
                        Assert.AreEqual(awaiter.GetResult(), result);
                    }
                }

                await acceptTask;
            }
        }
        #endregion
    }
}
