// Copyright
// ----------------------------------------------------------------------------------------------------------
//  <copyright file="BlockingBufferManagerTests.cs" company="https://github.com/safakgur/Dawn.SocketAwaitable">
//      MIT
//  </copyright>
//  <license>
//      This source code is subject to terms and conditions of The MIT License (MIT).
//      A copy of the license can be found in the License.txt file at the root of this distribution.
//  </license>
//  <summary>
//      Provides a class that contains unit tests for the blocking buffer manager.
//  </summary>
// ----------------------------------------------------------------------------------------------------------

namespace Dawn.Net.Sockets.Tests
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     Contains unit tests for <see cref="BlockingBufferManager" /> class.
    /// </summary>
    [TestClass]
    public class BlockingBufferManagerTests
    {
        #region Methods
        /// <summary>
        ///     Tests the constructor of <see cref="BlockingBufferManager" /> class.
        /// </summary>
        [TestMethod]
        public void TestInitialization()
        {
            int size = 300;
            int count = 4000;

            var manager = new BlockingBufferManager(size, count);
            Assert.AreEqual(manager.BufferSize, size);
            Assert.AreEqual(manager.AvailableBuffers, count);
        }

        /// <summary>
        ///     Tests the constructor of <see cref="BlockingBufferManager" /> class by specifying zero as size.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestInitializationUsingSizeZero()
        {
            int size = 0;
            int count = 4000;

            var manager = new BlockingBufferManager(size, count);
        }

        /// <summary>
        ///     Tests the constructor of <see cref="BlockingBufferManager" /> class by specifying zero as count.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestInitializationUsingCountZero()
        {
            int size = 300;
            int count = 0;

            var manager = new BlockingBufferManager(size, count);
        }

        /// <summary>
        ///     Tests the taking and releasing buffers in parallel.
        /// </summary>
        [TestMethod]
        public void TestTakingAndReleasing()
        {
            var count = 4000;
            var manager = new BlockingBufferManager(300, count);

            // Take all buffers.
            var buffers = new ArraySegment<byte>[count];
            Parallel.For(0, count, i => buffers[i] = manager.GetBuffer());
            Assert.AreEqual(manager.AvailableBuffers, 0);

            // Start releasing every buffer after one second.
            Task.Delay(1000).ContinueWith(t => Parallel.For(0, count, i => manager.ReleaseBuffer(buffers[i])));
            Assert.AreEqual(manager.AvailableBuffers, 0);

            // Take buffers as they become available, block the thread as needed.
            var buffers2 = new ArraySegment<byte>[count];
            Parallel.For(0, count, i => buffers2[i] = manager.GetBuffer());
            Assert.AreEqual(manager.AvailableBuffers, 0);
        }

        /// <summary>
        ///     Tests disposing the <see cref="BlockingBufferManager" /> and attempting to take a new buffer
        ///     after that.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestDisposingBeforeGettingBuffer()
        {
            var count = 4000;
            var manager = new BlockingBufferManager(300, count);

            manager.Dispose();
            manager.GetBuffer();
        }

        /// <summary>
        ///     Tests disposing the <see cref="BlockingBufferManager" /> and attempting to release a buffer
        ///     after that.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestDisposingBeforeReleasingBuffer()
        {
            var count = 4000;
            var manager = new BlockingBufferManager(300, count);

            var buffer = manager.GetBuffer();
            manager.Dispose();
            manager.ReleaseBuffer(buffer);
        }

        /// <summary>
        ///     Tests disposing the <see cref="BlockingBufferManager" /> while another thread was waiting for
        ///     a buffer to become available.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestDisposingWhileGettingBuffer()
        {
            var manager = new BlockingBufferManager(300, 1);

            // Take the only buffer.
            manager.GetBuffer();

            // Dispose after one second.
            Task.Delay(1000).ContinueWith(t => manager.Dispose());

            // Wait for a buffer to become available.
            manager.GetBuffer();
        }
        #endregion
    }
}