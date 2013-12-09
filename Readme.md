# <sup>Dawn.</sup>SocketAwaitable
A library that provides utilities for taking advantage of the async/await functionality on asynchronouss socket operations.

## Background
Prior to Task Parallel Library ([TPL](msdn.microsoft.com/en-us/library/dd460717.aspx)) that is introduced in .NET Framework 4, and [async/await](http://msdn.microsoft.com/en-us/library/hh191443.aspx) operators in C# 5 and VB 11, most of the asynchronous methods in the framework consisted of Begin/End pairs ([APM](msdn.microsoft.com/en-us/library/ms228963.aspx)) or were event-based ([EAP](msdn.microsoft.com/en-us/library/ms228969.aspx)).

In .NET 4.5 most of these methods now have Task-based alternatives that can be _awaited_ but unfortunately, [Socket](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.aspx) methods do not.

I started searching for alternatives since I was expecting awaitable socket methods in .NET Framework 4.5 but they were not there. I found [this great blog post](http://blogs.msdn.com/b/pfxteam/archive/2011/12/15/10248293.aspx) by [Stephen Toub](http://blogs.msdn.com/b/toub/) and there it was.

In his sample, Stephen created a SocketAwaitable class that takes a SocketAsyncEventArgs in its constructor and provides a GetAwaiter method in order to be awaitable. He also created a static, SocketHelpers class that provides extension methods for Socket. Each of these methods take a SocketAwaitable as parameter and returns the same SocketAwaitable after starting the asynchronous operation.

I took his sample and made a few changes to SocketAwaitable for it to replace SocketAsyncEventArgs completely. I also added a thread-safe collection class for pooling the awaitable arguments and a buffer manager that provides fixed-size buffers.

You can find more information about the changes I made in the Analysis section.

## Analysis
### SocketAwaitable Class
SocketAwaitable is a complete, awaitable alternative for SocketAsyncEventArgs.

#### Major Differences
These are the differences that seperate SocketAwaitable from SocketAsyncEventArgs the most.

1. **SocketAwaitable** is an awaitable class unlike SocketAsyncEventArgs. Therefore it doesn't expose a Completed event and provides a GetAwaiter method that returns a SocketAwaiter instead.
    ```csharp
    // SocketAsyncEventArgs:
    private void Connect(Socket s, EndPoint endPoint)
    {
        var e = new SocketAsyncEventArgs();
        e.RemoteEndPoint = endPoint;
        
        // OnCompleted is the name of the method to run when the operation is completed.
        e.Completed += OnCompleted;
        
        // If the asynchronous method returns false, than the operation is finished synchronously.
        // In this case, Completed event won't be triggered.
        if (!s.ConnectAsync(e))
            OnCompleted(this, e);
    }
    
    // SocketAwaitable:
    private async Task ConnectAsync(Socket s, EndPoint endPoint)
    {
        var a = new SocketAwaitable();
        a.RemoteEndPoint = endPoint;
        
        // ConnectAsync(SocketAwaitable) is an extension method defined in SocketEx class.
        // result is a SocketError value.
        var result = await s.ConnectAsync(a);
    }
    ```

2. SocketAwaitable has a boolean property called **ShouldCaptureContext** which, if set to true, causes the socket operations using the SocketAwaitable to capture the current synchronization context before they begin, and marshal the continuation back to the captured context.
    ```csharp
    private async Task ConnectAsync(Socket s, EndPoint endPoint)
    {
        var a = new SocketAwaitable();
        a.RemoteEndPoint = endPoint;
        
        // Default value of the SocketAwaitable.ShouldCaputreContext is false.
        a.ShouldCaptureContext = true;
        
        var result = await s.ConnectAsync(a);
        if (result == SocketError.Success)
        {
            // This line would throw an exception if ShouldCaptureContext was false.
            this.SomeTextBox.Text = "Success: " + endPoint;
        }
    }
    ```

3. SocketAwaitable has a **Clear** method that clears AcceptSocket, Buffer, RemoteEndPoint, SocketFlags and UserToken to prepare awaitable arguments for pooling.
    ```csharp
    private readonly SocketAwaitablePool pool = new SocketAwaitablePool();

    private async Task<bool> ConnectAsync(Socket s, EndPoint endPoint)
    {
        var a = this.pool.Take();
        a.RemoteEndPoint = endPoint;
        
        SocketError result;
        for (int i = 0; i < 3; i++)
        {
            result = await s.ConnectAsync(a);
            if (result == SocketError.Success)
                break;
        }
        
        a.Clear(); // Clears `a.RemoteEndPoint`.
        this.pool.Add(a);
        
        return result == SocketError.Success;
    }
    ```

#### Minor Differences
These are the minor differences between SocketAwaitable and SocketAsyncEventArgs classes.

1. **AcceptSocket** is a read-only member in SocketAwaitable.  
   Starting a new socket operation using the SocketAwaitable or calling Clear nullifies AcceptSocket.

    ```csharp
    private readonly SocketAwaitablePool pool = new SocketAwaitablePool();
    
    private async Tas<bool> AcceptAsync(Socket s)
    {
        var a = this.pool.Take();
        while (await s.AcceptAsync(a) == SocketError.Success)
            this.OnAccepted(a.AcceptSocket); // No need to call `a.AcceptSocket = null` every time.
        
        a.Clear(); // Clears AcceptSocket.
        this.pool.Add(a);
    }
    ```

2. Buffer, Offset and Count properties of SocketAsyncEventArgs are exposed as one ArraySegment&lt;byte&gt; property called **Buffer** in SocketAwaitable. It's Array property is never null and returns a static, empty array if no buffer is specified. SetBuffer also doesn't exist in SocketAwaitable. Calling Clear method clears Buffer.

   Like Buffer, BytesTransferred is also exposed as an ArraySegment&lt;byte&gt; property called **Transferred**, which provides the same array and offset with the used buffer but gives the number of the transferred bytes as count.
    ```csharp
    private readonly SocketAwaitablePool pool = new SocketAwaitablePool(10000);
    private readonly BufferManager bufferManager = new BufferManager(1024, 10000);
    
    private async Task MirrorAsync(Socket s)
    {
        var a = this.pool.Take(); // Take a `SocketAwaitable` from the pool.
        var b = this.bufferManager.GetBuffer(); // Take a buffer from the manager.
        a.Buffer = b; // Buffer is an ArraySegment<byte>.
        try
        {
            while (await s.ReceiveAsync(a) == SocketError.Success && a.Transferred.Count > 0)
            {
                a.Buffer = a.Transferred; // Set the buffer to send what is received.
                while (true)
                {
                    if (await s.SendAsync(a) != SocketError.Success)
                        return; // Return if can't send.
                    
                    if (a.Buffer.Count == a.Transferred.Count)
                        break; // Break if all the data is sent.
                    
                    // Set the buffer to send the remaining data.
                    a.Buffer = new ArraySegment<byte>(
                        a.Buffer.Array,
                        a.Buffer.Offset + a.Transferred.Count,
                        a.Buffer.Count - a.Transferred.Count);
                }
                
                a.Buffer = b; // Set the original buffer back to continue receiving.
            }
        }
        finally
        {
            a.Clear(); // Clear the awaitable arguments.
            this.pool.Add(a); // Add the `SocketAwaitable` back to the pool.
            this.bufferManager.ReleaseBuffer(b); // Release the buffer.
        }
    }
    ```

3. **BufferList** is not supported in SocketAwaitable. One reason for this is that SocketAsyncEventArgs copies the specified list into an array on assignment and internally, uses the copied array. This means the methods that manipulate the list like `e.BufferList.Add(buffer)` doesn't work, which I think is a bad decision regarding API design. Also, using BlockingBufferManager class to manage buffers makes using BufferList redundant.
4. **ConnectSocket** is not supported in SocketAwaitable, since it has no use in an awaitable class like it has in SocketAsyncEventArgs.
5. **SocketClientAccessPolicyProtocol** is not supported in SocketAwaitable, since it's already marked with ObsoleteAttribute and there is no point in exposing it.
6. **SocketError** is not supported in SocketAwaitable, since it is the return type of SocketAwaiter.GetResult. That means the users can check the result of every asynchronous socket operation, right after awaiting the operation. Therefore, there is no need to have SocketError as a property of SocketAwaitable.

#### Exposed Directly
These are the features of SocketAsyncEventArgs that are exposed by SocketAwaitable directly.

* **ConnectByNameError** property
* **DisconnectReuseSocket** property
* **LastOperation** property
* **RemoteEndPoint** property
* **SocketFlags** property
* **UserToken** property (may be removed in future)
* **Dispose** method

#### Currently Unsupported
These are the features of SocketAsyncEventArgs that are not yet supported by the SocketAwaitable class.

* **ReceiveMessageFromPacketInfo** property
* **SendPacketsElements** property
* **SendPacketsFlags** property
* **SendPacketsSendSize** property

### SocketAwaiter Class
**SocketAwaiter** class is used to await SocketAwaitable objects. Every SocketAwaitable has its own SocketAwaiter and multiple calls to one SocketAwaitable's **GetAwaiter** method will cause the same SocketAwaiter instance to return every time.

```csharp
var a = new SocketAwaitable();
Object.ReferenceEquals(a.GetAwaiter(), a.GetAwaiter()); // Returns true.
```

**GetResult** method of the SocketAwaiter class returns a **SocketError** value that represents the result of the awaited operation. It's default value is SocketError.Success and it returns SocketError.AlreadyInProgress while an asynchronous socket operation is already in progress. After the first operation, it returns the result of the last operation.

Default value of the **SocketAwaiter.IsCompleted** property is true and SocketAwaiter resets it to true after completing an operation, successfuly or not.

```csharp
private async Task TestConnectAsync(Socket s, EndPoint endPoint)
{
    var a = new SocketAwaitable();
    var w = a.GetAwaiter();
    // `w.IsCompleted` is true.
    // `w.GetResult()` and `await a` returns SocketError.Success.
    
    var result = await s.ConnectAsync(a);
    // `w.IsCompleted` is true.
    // `w.GetResult()` and `await a` return the same value as `result`.
}
```

### SocketEx Class
**SocketEx** is a static class that provides awaitable extension methods for Socket that require awaitable socket arguments (SocketAwaitable). SocketEx methods return the specified SocketAwaitable objects directly.

That means every method of the SocketEx class returns a SocketError when awaited.

#### Supported
These are the names of the Socket methods that have awaitable alternatives provided by  
SocketEx class. 

* **AcceptAsync**
* **ConnectAsync**
* **DisconnectAsync**
* **ReceiveAsync**
* **SendAsync**

#### Currently Unsupported
These are the names of the Socket methods that don't have any awaitable alternatives  
provided by SocketEx class yet.

* **CancelConnectAsync**
* **ReceiveFromAsync**
* **ReceiveMessageFromAsync**
* **SendPacketsAsync**
* **SendToAsync**

### SocketAwaitablePool Class
**SocketAwaitablePool** is a thread-safe collection class for pooling SocketAwaitable instances. If depleted, the pool initializes a new SocketAwaitable object when its Take method is called and returns the new item.

After using a SocketAwaitable object, the awaitable can be added back to its pool by calling the pool's Add method. SocketAwaitable.Clear must be called before adding an awaitable back to a pool.

```csharp
// Initialize a pool with 10 `SocketAwaitable`s in it.
var pool = new SocketAwaitablePool(10); // `pool.Count` is 10.

var temp = new SocketAwaitable[pool.Count + 2];

// Take all the awaitables from the pool.
for (int i = 0; i < pool.Count; i++)
    temp[i] = pool.Take();

// `pool.Count` is 0. `SocketAwaitable.Take()` returns a new SocketAwaitable at this point.
temp[pool.Count] = pool.Take();     // `pool.Count` is 0.
temp[pool.Count + 1] = pool.Take(); // `pool.Count` is still 0.

// Add all the awaitables taken from the pool, back to the pool.
for (int i = 0; i < temp.Length; i++)
    pool.Add(temp[i]);

// `pool.Count` is 12.
```

### BlockingBufferManager Class
**BlockingBufferManager** is a thread-safe class which creates a one big data block and provides its
parts as buffers. This is useful for asynchronous socket programming because:

1. Keeping one big block of memory instead of many small blocks avoids fragmentation.
2. Most asynchronous I/O operations in .NET Framework use the I/O completion ports (IOCP) which is a very low level, unmanaged thread-pool that requires buffers to be pinned to the memory in order to access them and this forces buffers to be re-usable.

If no free buffer is left, BlockingBufferManager.GetBuffer blocks the calling thread until a buffer is released.

```csharp
var manager = new BlockingBufferManager(
    1024, // Buffers should be 1 kB each.
    10000); // 10000 buffers will be used, concurrently.
// `manager` holds a single data block of 1024 * 10000 bytes (10 MB).
// `manager.BufferSize` is 1024.
// `manager.AvailableBuffers` is 10000.

var buffer1 = manager.GetBuffer(); // `buffer1.Count` is `manager.BufferSize` (1024).
var buffer2 = manager.GetBuffer(); // `buffer2.Count` is also `manager.BufferSize`.
// `buffer1.Array == buffer2.Array` is true.
// `manager.AvailableBuffers` is 9998.

var buffers = new ArraySegment<byte>[10000];
buffers[0] = buffer1;
buffers[1] = buffer2;
for (int i = 2; i < manager.AvailableBuffers; i++)
    buffers[i] = manager.GetBuffer();
// `manager.AvailableBuffers` is 0.
// Every element in `buffers` has the same `Array` and `Count`.

// Wait one second and release the first buffer in `buffers`.
Task.Delay(1000).ContinueWith(t => manager.ReleaseBuffer(buffers[0]));

// Since there is no buffer available, `GetBuffer` will block the calling thread until a buffer is
// released which will happen in ~1 second.
var buffer3 = manager.GetBuffer();
```

## Contributions
You can create a pull request if you're interested in contributing the project.

1. All the classes must comply with [StyleCop](http://stylecop.codeplex.com) rules. (Yes, I have some [exceptions](src/Settings.StyleCop))
2. I use [SemVer](http://semver.org) and Vincent Driessen's [branching model](http://nvie.com/posts/a-successful-git-branching-model/).

You can also create an [issue](https://github.com/safakgur/Dawn.SocketAwaitable/issues/new) or [send me an e-mail](mailto:safak9ur@gmail.com) for feature requests.

## License
See [License.txt](License.txt).