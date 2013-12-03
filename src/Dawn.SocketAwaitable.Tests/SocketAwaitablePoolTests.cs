// Copyright
// ----------------------------------------------------------------------------------------------------------
//  <copyright file="SocketAwaitablePoolTests.cs" company="https://github.com/safakgur/Dawn.SocketAwaitable">
//      MIT
//  </copyright>
//  <license>
//      This source code is subject to terms and conditions of The MIT License (MIT).
//      A copy of the license can be found in the License.txt file at the root of this distribution.
//  </license>
//  <summary>
//      Provides a class that contains unit tests for the awaitable socket argument pool.
//  </summary>
// ----------------------------------------------------------------------------------------------------------

namespace Dawn.Net.Sockets.Tests
{
    using System;
    using System.Collections;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    ///     Contains unit tests for <see cref="SocketAwaitablePool" /> class.
    /// </summary>
    [TestClass]
    public class SocketAwaitablePoolTests
    {
        #region Methods
        /// <summary>
        ///     Tests the constructor of <see cref="SocketAwaitablePool" /> class.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestInitialization()
        {
            int count = 20;

            // Initialize using a valid count.
            var pool = new SocketAwaitablePool(count);
            Assert.AreEqual(pool.Count, count);

            // Initialize using the default count.
            pool = new SocketAwaitablePool();
            Assert.AreEqual(pool.Count, 0);

            // Initialize using an invalid count.
            pool = new SocketAwaitablePool(-1);
        }

        /// <summary>
        ///     Tests the <see cref="SocketAwaitablePool.Add" /> method by specifying a null argument.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestAddingNull()
        {
            new SocketAwaitablePool(0).Add(null);
        }

        /// <summary>
        ///     Tests the core functionality of <see cref="SocketAwaitablePool" /> class.
        /// </summary>
        [TestMethod]
        public void TestConcurrentAccess()
        {
            int count = 20;

            // Initialize using a specific count.
            var pool = new SocketAwaitablePool(count);
            Assert.AreEqual(pool.Count, count);

            // Deplete the pool.
            Parallel.For(0, count, i => Assert.IsNotNull(pool.Take()));
            Assert.AreEqual(pool.Count, 0);

            // Force pool to initialize new awaitables.
            var newAwaitables = new SocketAwaitable[count];
            Parallel.For(0, count, i => Assert.IsNotNull(newAwaitables[i] = pool.Take()));

            // Add new awaitables [back] to the pool.
            Parallel.For(0, count, i => pool.Add(newAwaitables[i]));
            Assert.AreEqual(pool.Count, count);

            // Add to, take from and iterate the pool in parallel.
            var addTask = Task.Run(
                () => Parallel.For(0, 1000000, i => pool.Add(new SocketAwaitable())));

            var takeTask = Task.Run(
                () => Parallel.For(0, 1000000 + count, i => Assert.IsNotNull(pool.Take())));

            var iterateTask = Task.Run(
                () => Parallel.ForEach(pool, e => Assert.IsNotNull(e)));

            Task.WaitAll(addTask, takeTask, iterateTask);
        }

        /// <summary>
        ///     Tests the <see cref="ICollection.IsSynchronized" /> and <see cref="ICollection.SyncRoot" />
        ///     properties of the <see cref="SocketAwaitablePool" /> class.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void TestLockingSyncRoot()
        {
            var r = new Random();
            var p = new SocketAwaitablePool(r.Next(1000000)) as ICollection;

            Assert.AreEqual(p.IsSynchronized, false);
            lock (p.SyncRoot)
            {
                Assert.Fail("This should never execute.");
            }
        }

        /// <summary>
        ///     Tests the <see cref="ICollection.CopyTo" /> method of the <see cref="SocketAwaitablePool" />
        ///     class, by specifying valid arguments.
        /// </summary>
        [TestMethod]
        public void TestCopyingToValidArray()
        {
            var p = new SocketAwaitablePool(1) as ICollection;
            var a = new SocketAwaitable[1];

            p.CopyTo(a, 0);
            Assert.IsNotNull(a[0]);
        }

        /// <summary>
        ///     Tests the <see cref="ICollection.CopyTo" /> method of the <see cref="SocketAwaitablePool" />
        ///     class, by specifying a null array.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void TestCopyingToNullArray()
        {
            var p = new SocketAwaitablePool() as ICollection;
            p.CopyTo(null, 0);
        }

        /// <summary>
        ///     Tests the <see cref="ICollection.CopyTo" /> method of the <see cref="SocketAwaitablePool" />
        ///     class, by specifying a negative index.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void TestCopyingUsingNegativeIndex()
        {
            var p = new SocketAwaitablePool(1) as ICollection;
            p.CopyTo(new SocketAwaitable[1], -1);
        }

        /// <summary>
        ///     Tests the <see cref="ICollection.CopyTo" /> method of the <see cref="SocketAwaitablePool" />
        ///     class, by specifying an array of integers.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestCopyingToInt32Array()
        {
            var p = new SocketAwaitablePool(1) as ICollection;
            p.CopyTo(new int[1], 0);
        }

        /// <summary>
        ///     Tests the <see cref="ICollection.CopyTo" /> method of the <see cref="SocketAwaitablePool" />
        ///     class, by specifying a multi-dimensional array.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestCopyingToMultiDimensionalArray()
        {
            var p = new SocketAwaitablePool(1) as ICollection;
            p.CopyTo(new SocketAwaitable[1, 1], 0);
        }

        /// <summary>
        ///     Tests the <see cref="ICollection.CopyTo" /> method of the <see cref="SocketAwaitablePool" />
        ///     class, by specifying a jagged array.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestCopyingToJaggedArray()
        {
            var p = new SocketAwaitablePool(1) as ICollection;
            p.CopyTo(new SocketAwaitable[1][], 0);
        }

        /// <summary>
        ///     Tests the <see cref="ICollection.CopyTo" /> method of the <see cref="SocketAwaitablePool" />
        ///     class, by specifying an array that is too small.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void TestCopyingToSmallArray()
        {
            var p = new SocketAwaitablePool(2) as ICollection;
            p.CopyTo(new SocketAwaitable[1], 0);
        }

        /// <summary>
        ///     Tests <see cref="SocketAwaitablePool.Add" /> after disposing the pool.
        /// </summary>
        [TestMethod]
        public void TestAddingAfterDispose()
        {
            var count = 20;
            var pool = new SocketAwaitablePool(count);
            Assert.AreEqual(pool.Count, count);

            pool.Dispose();
            Assert.AreEqual(pool.Count, 0);

            var awaitable = new SocketAwaitable();
            Assert.IsFalse(awaitable.IsDisposed);

            pool.Add(awaitable);
            Assert.IsTrue(awaitable.IsDisposed);
        }

        /// <summary>
        ///     Tests <see cref="SocketAwaitablePool.Take" /> after disposing the pool.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestTakingAfterDispose()
        {
            var pool = new SocketAwaitablePool();
            pool.Dispose();
            pool.Take();
        }

        /// <summary>
        ///     Tests <see cref="SocketAwaitablePool.GetEnumerator" /> after disposing the pool.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestIteratingAfterDispose()
        {
            var pool = new SocketAwaitablePool();
            pool.Dispose();
            foreach (var awaitable in pool)
            {
            }
        }

        /// <summary>
        ///     Tests <see cref="ICollection.CopyTo" /> methods of the <see cref="SocketAwaitablePool" />
        ///     after disposing the pool.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ObjectDisposedException))]
        public void TestCopyingAfterDispose()
        {
            var pool = new SocketAwaitablePool(1);
            pool.Dispose();

            var array = new SocketAwaitable[1];
            (pool as ICollection).CopyTo(array, 0);
        }
        #endregion
    }
}