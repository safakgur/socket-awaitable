# <sup>Dawn.</sup>SocketAwaitable
An easy-to-use library for CLR that provides utilities for taking advantage of the `async`/`await` functionality on asynchronouss socket operations.

## History
Prior to [Task Parallel Library (TPL)](msdn.microsoft.com/en-us/library/dd460717.aspx) that is introduced in .NET Framework 4, and [`async`/`await`](http://msdn.microsoft.com/en-us/library/hh191443.aspx) support in C# 5 and VB 11, most of the asynchronous methods in the framework consisted of `Begin*`/`End*` pairs ([APM](msdn.microsoft.com/en-us/library/ms228963.aspx)) or were event-based ([EAP](msdn.microsoft.com/en-us/library/ms228969.aspx)). But with .NET Framework 4.5, most of these methods now have Task based alternatives that can be _awaited_ but unfortunately, `Socket` methods don't.

I started searching for alternatives since I was expecting awaitable socket methods in .NET Framework 4.5 and, you know, they were not there. But I found [this great blog post](http://blogs.msdn.com/b/pfxteam/archive/2011/12/15/10248293.aspx) by [Stephen Toub](http://blogs.msdn.com/b/toub/) -who happens to be a great guy- and there it was.

So I took Stephen's sample and made a few changes like refactoring the code for [StyleCop](http://stylecop.codeplex.com) specifications, splitting the awaiter into another class and changing its `GetResult` method to return a `SocketError` value; along with a few additions like a thread-safe pool and a buffer manager.

## Usage
### `SocketAwaitable`, `SocketEx` and `SocketAwaitablePool`
```csharp
// Initializes 10000 `SocketAwaitable` instances for the pool.
private readonly SocketAwaitablePool awaitables = new SocketAwaitablePool(10000);

private async Task AcceptAsync(Socket s)
{
    // Take a `SocketAwaitable` from the pool.
    // The pool will initialize a new `SocketAwaitable` if depleted.
    var a = this.awaitables.Take();
    try
    {
        // Awaiting a `SocketAwaitable` clears its `AcceptSocket` property.
        // So there is no need to write `a.AcceptSocket = null` each time you accept.
        while (await s.AcceptAsync(a) == SocketError.Success)
            this.Register(a.AcceptSocket);
    }
    finally
    {
        // Calling `SocketAwaitable.Clear` clears `AcceptSocket`, `Buffer`, `RemoteEndPoint`,
        // `UserToken` and `SocketFlags`. You must call `Clear` before you add a `SocketAwaitable`
        // back to the pool.
        a.Clear();
        this.awaitables.Add(a);
    }
}
```

### `BlockingBufferManager`
```csharp
// Initializes an underlying `byte[1024 * 200]` and provides 200 buffers as array segments.
private readonly BlockingBufferManager bufferManager = new BlockingBufferManager(1024, 200);

private async Task MirrorAsync(Socket s)
{
    // `BlockingBufferManager.GetBuffer` blocks the calling thread if it doesn't have an available
    // buffer until a buffer is released. So make sure you allocate a large enough data block when
    // you initialize a `BlockingBufferManager` and release the buffers when their jobs are done.
    var b = this.bufferManager.GetBuffer(); // `b.Count` is 1024.
    var a = this.awaitables.Take();
    a.Buffer = b; // `SocketAwaitable.Buffer` is an array segment.
    try
    {
        while (await s.ReceiveAsync(a) == SocketError.Success && a.Transferred.Count > 0)
        {
            a.Buffer = a.Transferred;  // `SocketAwaitable.Transferred` also is an array segment.
            while (true)
            {
                if (await s.SendAsync(a) != SocketError.Success)
                    return;

                if (a.Buffer.Count == a.Transferred.Count)
                    break;

                a.Buffer = new ArraySegment<byte>(
                    a.Buffer.Array,
                    a.Buffer.Offset + a.Transferred.Count,
                    a.Buffer.Count - a.Transferred.Count);
            }

            a.Buffer = b;
        }
    }
    finally
    {
        this.bufferManager.ReleaseBuffer(b);

        a.Clear();
        this.awaitables.Add(a);
    }
}
```

## License
See [License.txt](License.txt).