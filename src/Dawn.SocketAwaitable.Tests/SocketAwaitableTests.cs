// Copyright
// ----------------------------------------------------------------------------------------------------------
//  <copyright file="SocketAwaitableTests.cs" company="https://github.com/safakgur/Dawn.SocketAwaitable">
//      MIT
//  </copyright>
//  <license>
//      This source code is subject to terms and conditions of The MIT License (MIT).
//      A copy of the license can be found in the License.txt file at the root of this distribution.
//  </license>
//  <summary>
//      Provides a class that contains unit tests for awaitable socket arguments.
//  </summary>
// ----------------------------------------------------------------------------------------------------------

namespace Dawn.Net.Sockets.Tests
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     Contains unit tests for <see cref="SocketAwaitable" /> class.
    /// </summary>
    [TestClass]
    public class SocketAwaitableTests
    {
        #region Methods
        /// <summary>
        ///     Tests the constructor of <see cref="SocketAwaitable" /> class.
        /// </summary>
        [TestMethod]
        public void TestInitialization()
        {
            // Default values.
            var awaitable = new SocketAwaitable();
            Assert.IsNull(awaitable.AcceptSocket);
        }

        /// <summary>
        ///     Tests <see cref="SocketAwaitable.Buffer" />.
        /// </summary>
        [TestMethod]
        public void TestBuffer()
        {
            // Default buffer values.
            var awaitable = new SocketAwaitable();
            Assert.IsNotNull(awaitable.Buffer.Array);
            Assert.AreEqual(awaitable.Buffer.Array.Length, 0);
            Assert.AreEqual(awaitable.Buffer.Offset, 0);
            Assert.AreEqual(awaitable.Buffer.Count, 0);

            // Default transferred values.
            Assert.IsNotNull(awaitable.Transferred.Array);
            Assert.AreEqual(awaitable.Transferred.Array.Length, 0);
            Assert.AreEqual(awaitable.Transferred.Offset, 0);
            Assert.AreEqual(awaitable.Transferred.Count, 0);

            // Assign a null buffer.
            var nullBuffer = default(ArraySegment<byte>);
            Assert.IsNull(nullBuffer.Array);

            awaitable.Buffer = nullBuffer;
            Assert.IsNotNull(awaitable.Buffer.Array);
            Assert.AreEqual(awaitable.Buffer.Array.Length, 0);
            Assert.AreEqual(awaitable.Buffer.Offset, 0);
            Assert.AreEqual(awaitable.Buffer.Count, 0);
            
            // Assign a valid buffer.
            var data = new byte[32];
            var buffer = new ArraySegment<byte>(data, 8, 16);

            awaitable.Buffer = buffer;
            Assert.AreSame(awaitable.Buffer.Array, data);
            Assert.AreEqual(awaitable.Buffer.Offset, buffer.Offset);
            Assert.AreEqual(awaitable.Buffer.Count, buffer.Count);

            // Clear awaitable.
            awaitable.Clear();
            Assert.IsNotNull(awaitable.Buffer.Array);
            Assert.AreEqual(awaitable.Buffer.Array.Length, 0);
            Assert.AreEqual(awaitable.Buffer.Offset, 0);
            Assert.AreEqual(awaitable.Buffer.Count, 0);
        }

        /// <summary>
        ///     Tests <see cref="SocketAwaitable.RemoteEndPoint" />.
        /// </summary>
        [TestMethod]
        public void TestRemoteEndPoint()
        {
            // Default value.
            var awaitable = new SocketAwaitable();
            Assert.IsNull(awaitable.RemoteEndPoint);

            // Assign value.
            var endPoint = new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort);
            awaitable.RemoteEndPoint = endPoint;
            Assert.AreSame(awaitable.RemoteEndPoint, endPoint);

            // Clear awaitable.
            awaitable.Clear();
            Assert.IsNull(awaitable.RemoteEndPoint);
        }

        /// <summary>
        ///     Tests <see cref="SocketAwaitable.SocketFlags" />.
        /// </summary>
        [TestMethod]
        public void TestSocketFlags()
        {
            // Default value.
            var awaitable = new SocketAwaitable();
            Assert.AreEqual(awaitable.SocketFlags, SocketFlags.None);

            // Assign value.
            awaitable.SocketFlags = SocketFlags.Broadcast;
            Assert.AreEqual(awaitable.SocketFlags, SocketFlags.Broadcast);

            // Clear awaitable.
            awaitable.Clear();
            Assert.AreEqual(awaitable.SocketFlags, SocketFlags.None);
        }

        /// <summary>
        ///     Tests <see cref="SocketAwaitable.UserToken" />.
        /// </summary>
        [TestMethod]
        public void TestUserToken()
        {
            // Default value.
            var awaitable = new SocketAwaitable();
            Assert.IsNull(awaitable.UserToken);

            // Assign value.
            var token = new { Property = "Value" };
            awaitable.UserToken = token;
            Assert.AreSame(awaitable.UserToken, token);

            // Clear awaitable.
            awaitable.Clear();
            Assert.IsNull(awaitable.UserToken);
        }

        /// <summary>
        ///     Tests <see cref="SocketAwaitable.GetAwaiter" />.
        /// </summary>
        [TestMethod]
        public void TestGettingAwaiter()
        {
            var awaitable = new SocketAwaitable();
            var awaiter = awaitable.GetAwaiter();
            Assert.IsTrue(awaiter.IsCompleted);
            Assert.AreEqual(awaiter.GetResult(), default(SocketError));
        }

        /// <summary>
        ///     Tests <see cref="SocketAwaitable.Dispose" />.
        /// </summary>
        [TestMethod]
        public void TestDisposing()
        {
            var awaitable = new SocketAwaitable();
            Assert.IsFalse(awaitable.IsDisposed);

            awaitable.Dispose();
            Assert.IsTrue(awaitable.IsDisposed);
        }
        #endregion
    }
}