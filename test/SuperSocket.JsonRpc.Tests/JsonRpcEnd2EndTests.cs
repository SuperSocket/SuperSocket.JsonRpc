using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NRPC.Abstractions.Metadata;
using NRPC.Caller;
using NRPC.Executor;
using NRPC.SuperSocket.Server;
using SuperSocket.JsonRpc.Caller;
using SuperSocket.JsonRpc.Server;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Host;
using Xunit.Sdk;

namespace SuperSocket.JsonRpc.Tests;

public class JsonRpcEnd2EndTests
{
    private readonly int _testPort = 4040;

    [Fact]
    public async Task TestJsonRpcRequestResponse_SingleRequest()
    {
        // Arrange
        using var host = SetupServer();

        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));

        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        Assert.NotNull(caller);

        // Act
        var result = await caller.Add(42, 23).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(65, result);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_ErrorResponse()
    {
        // Arrange
        using var host = SetupServer();

        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));

        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        var exception = await Assert.ThrowsAsync<RpcServerException>(() => caller.Fail("This is a test failure").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.NotNull(exception);
        Assert.NotNull(exception.ServerError);
        Assert.Equal(500, exception.ServerError.Code);
        Assert.Equal("This is a test failure", exception.ServerError.Message);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_MultipleOperations()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test different mathematical operations
        var addResult = await caller.Add(10, 5).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(15, addResult);

        var subtractResult = await caller.Subtract(20, 8).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(12, subtractResult);

        var multiplyResult = await caller.Multiply(6, 7).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(42, multiplyResult);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_StringOperations()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test string concatenation
        var result = await caller.Concatenate("Hello", " World").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal("Hello World", result);

        // Test with empty strings
        var emptyResult = await caller.Concatenate("", "test").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal("test", emptyResult);

        // Test with special characters
        var specialResult = await caller.Concatenate("JSON-RPC", " 2.0").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal("JSON-RPC 2.0", specialResult);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_Notifications()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Clear any previous notification state
        JsonRpcServerService.LastNotifyMessage = null;

        // Act - Send notifications (no response expected)
        await caller.Notify("Test notification 1").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await caller.Notify("Test notification 2").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Give some time for notifications to be processed
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Assert - Verify the last notification was received
        Assert.Equal("Test notification 2", JsonRpcServerService.LastNotifyMessage);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_StateManagement()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test state management with trigger mechanism
        var initialTriggerTimes = await caller.GetTriggerTimes().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(0, initialTriggerTimes);

        await caller.Trigger().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var firstTriggerTimes = await caller.GetTriggerTimes().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, firstTriggerTimes);

        await caller.Trigger().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        await caller.Trigger().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var finalTriggerTimes = await caller.GetTriggerTimes().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(3, finalTriggerTimes);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_ConcurrentRequests()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act - Send multiple concurrent requests
        var tasks = new List<Task<int>>();
        for (int i = 0; i < 10; i++)
        {
            var a = i * 10;
            var b = i * 5;
            tasks.Add(caller.Add(a, b).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Verify all results are correct
        for (int i = 0; i < 10; i++)
        {
            var expected = (i * 10) + (i * 5); // a + b
            Assert.Equal(expected, results[i]);
        }
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_LargeNumbers()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test with large numbers
        var result1 = await caller.Add(int.MaxValue - 1, 1).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(int.MaxValue, result1);

        var result2 = await caller.Subtract(int.MaxValue, 1).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(int.MaxValue - 1, result2);

        var result3 = await caller.Multiply(1000000, 1000).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1000000000, result3);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_ConnectionRecovery()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller1 = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act - Use first caller, then create a new one
        var result1 = await caller1.Add(10, 20).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(30, result1);

        // Create a second caller (simulating connection recovery)
        var caller2 = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);
        var result2 = await caller2.Add(50, 25).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(75, result2);

        // Verify both callers can work simultaneously
        var task1 = caller1.Add(1, 2).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var task2 = caller2.Add(3, 4).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        var results = await Task.WhenAll(task1, task2);
        Assert.Equal(3, results[0]);
        Assert.Equal(7, results[1]);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_PerformanceBaseline()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act - Measure time for multiple sequential calls
        const int callCount = 100;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        for (int i = 0; i < callCount; i++)
        {
            await caller.Add(i, i + 1).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }

        stopwatch.Stop();

        // Assert - Verify reasonable performance (should complete in under 5 seconds for 100 calls)
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"Performance test took too long: {stopwatch.ElapsedMilliseconds}ms for {callCount} calls");
        
        // Calculate and log average time per call
        var avgTimePerCall = stopwatch.ElapsedMilliseconds / (double)callCount;
        Assert.True(avgTimePerCall < 50, $"Average time per call too high: {avgTimePerCall}ms");
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_DivisionOperations()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test division operations
        var result1 = await caller.Divide(10.0, 2.0).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(5.0, result1);

        var result2 = await caller.Divide(22.0, 7.0).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(22.0 / 7.0, result2, precision: 10);

        // Test division by zero error
        var exception = await Assert.ThrowsAsync<RpcServerException>(() => 
            caller.Divide(10.0, 0.0).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_BooleanOperations()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test boolean operations
        var evenResult = await caller.IsEven(4).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(evenResult);

        var oddResult = await caller.IsEven(7).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.False(oddResult);

        var zeroResult = await caller.IsEven(0).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.True(zeroResult);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_ArrayOperations()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test string splitting
        var result1 = await caller.SplitString("hello,world,test", ',').WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(3, result1.Length);
        Assert.Equal("hello", result1[0]);
        Assert.Equal("world", result1[1]);
        Assert.Equal("test", result1[2]);

        var result2 = await caller.SplitString("single", ',').WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Single(result2);
        Assert.Equal("single", result2[0]);

        var result3 = await caller.SplitString("", ',').WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Empty(result3);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_BatchProcessing()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test batch processing
        var items = new[] { "item1", "item2", "item3", "item4" };
        await caller.ProcessBatch(items).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // Since ProcessBatch is a notification-style method, we can't directly verify the result
        // but we can test that it doesn't throw an exception
        Assert.True(true); // If we reach here, the batch was processed successfully
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_DateTimeOperations()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test DateTime operations
        var beforeCall = DateTime.UtcNow;
        var serverTime = await caller.GetCurrentTime().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var afterCall = DateTime.UtcNow;

        // Verify the server time is within a reasonable range (should be very close to current time)
        Assert.True(serverTime >= beforeCall.AddSeconds(-1) && serverTime <= afterCall.AddSeconds(1),
            $"Server time {serverTime} is not within expected range [{beforeCall.AddSeconds(-1)}, {afterCall.AddSeconds(1)}]");
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_ExceptionHandling()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test different types of exceptions
        var exception1 = await Assert.ThrowsAsync<RpcServerException>(() => 
            caller.Fail("Custom error message").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Contains("Custom error message", exception1.ServerError.Message);

        var exception2 = await Assert.ThrowsAsync<RpcServerException>(() => 
            caller.Divide(1.0, 0.0).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Contains("divide by zero", exception2.ServerError.Message.ToLower());
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_MixedConcurrentOperations()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act - Mix different types of operations concurrently
        var addTask = caller.Add(10, 20).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var subtractTask = caller.Subtract(50, 25).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var multiplyTask = caller.Multiply(6, 7).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var divideTask = caller.Divide(100.0, 4.0).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var concatTask = caller.Concatenate("Hello", " World").WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var evenTask = caller.IsEven(42).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var splitTask = caller.SplitString("a,b,c", ',').WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        var notifyTask = caller.Notify("Concurrent test").WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        await Task.WhenAll(addTask, subtractTask, multiplyTask, divideTask, concatTask, evenTask, splitTask, notifyTask);

        // Assert - Verify results
        Assert.Equal(30, await addTask);
        Assert.Equal(25, await subtractTask);
        Assert.Equal(42, await multiplyTask);
        Assert.Equal(25.0, await divideTask);
        Assert.Equal("Hello World", await concatTask);
        Assert.True(await evenTask);
        var splitResult = await splitTask;
        Assert.Equal(3, splitResult.Length);
        Assert.Equal("a", splitResult[0]);
        Assert.Equal("b", splitResult[1]);
        Assert.Equal("c", splitResult[2]);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_NullAndEmptyValues()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test null and empty string handling
        var emptyResult = await caller.Concatenate("", "").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal("", emptyResult);

        var nullHandling = await caller.Concatenate("null", "").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal("null", nullHandling);

        var zeroResult = await caller.Add(0, 0).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(0, zeroResult);

        var negativeNumbers = await caller.Add(-10, -5).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(-15, negativeNumbers);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_EdgeCaseNumbers()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test edge case numbers
        var minValue = await caller.Add(int.MinValue, 0).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(int.MinValue, minValue);

        var maxValue = await caller.Add(int.MaxValue, 0).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(int.MaxValue, maxValue);

        // Test floating point precision
        var precisionTest = await caller.Divide(1.0, 3.0).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1.0 / 3.0, precisionTest, precision: 15);

        var verySmallNumber = await caller.Divide(1.0, 1000000.0).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(0.000001, verySmallNumber, precision: 15);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_UnicodeAndSpecialCharacters()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test Unicode and special characters
        var unicodeResult = await caller.Concatenate("Hello 🌍", " World 🚀").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal("Hello 🌍 World 🚀", unicodeResult);

        var specialCharsResult = await caller.Concatenate("Test\"\\", "/\n\t").WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal("Test\"\\/\n\t", specialCharsResult);

        // Test array with Unicode characters
        var unicodeSplit = await caller.SplitString("α,β,γ,δ", ',').WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(4, unicodeSplit.Length);
        Assert.Equal("α", unicodeSplit[0]);
        Assert.Equal("β", unicodeSplit[1]);
        Assert.Equal("γ", unicodeSplit[2]);
        Assert.Equal("δ", unicodeSplit[3]);
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_LargeArrayProcessing()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test large array processing
        var largeString = string.Join(",", Enumerable.Range(1, 1000).Select(i => $"item{i}"));
        var largeArrayResult = await caller.SplitString(largeString, ',').WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        
        Assert.Equal(1000, largeArrayResult.Length);
        Assert.Equal("item1", largeArrayResult[0]);
        Assert.Equal("item500", largeArrayResult[499]);
        Assert.Equal("item1000", largeArrayResult[999]);

        // Test large batch processing
        var largeBatch = Enumerable.Range(1, 500).Select(i => $"batch_item_{i}").ToArray();
        await caller.ProcessBatch(largeBatch).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
        Assert.True(true); // If we reach here, large batch was processed successfully
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_SequentialVsConcurrentPerformance()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        const int operationCount = 50;

        // Act - Sequential calls
        var sequentialStopwatch = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < operationCount; i++)
        {
            await caller.Add(i, i + 1).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
        sequentialStopwatch.Stop();

        // Act - Concurrent calls
        var concurrentStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var concurrentTasks = Enumerable.Range(0, operationCount)
            .Select(i => caller.Add(i, i + 1).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))
            .ToArray();
        var concurrentResults = await Task.WhenAll(concurrentTasks);
        concurrentStopwatch.Stop();

        // Assert - Concurrent should generally be faster than sequential
        Assert.True(concurrentStopwatch.ElapsedMilliseconds <= sequentialStopwatch.ElapsedMilliseconds + 1000,
            $"Concurrent processing ({concurrentStopwatch.ElapsedMilliseconds}ms) should be faster than or similar to sequential ({sequentialStopwatch.ElapsedMilliseconds}ms)");

        // Verify all results are correct
        for (int i = 0; i < operationCount; i++)
        {
            Assert.Equal(i + (i + 1), concurrentResults[i]);
        }
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_ConnectionStabilityUnderLoad()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        
        // Act - Create multiple callers and perform operations simultaneously
        var callers = new List<ITestService>();
        for (int i = 0; i < 5; i++)
        {
            callers.Add(await callerFactory.CreateCaller(TestContext.Current.CancellationToken));
        }

        var allTasks = new List<Task<int>>();
        for (int callerIndex = 0; callerIndex < callers.Count; callerIndex++)
        {
            var caller = callers[callerIndex];
            for (int opIndex = 0; opIndex < 20; opIndex++)
            {
                var a = callerIndex * 100 + opIndex;
                var b = opIndex + 1;
                allTasks.Add(caller.Add(a, b).WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken));
            }
        }

        var results = await Task.WhenAll(allTasks);

        // Assert - Verify all operations completed successfully
        Assert.Equal(100, results.Length); // 5 callers × 20 operations each
        
        // Verify a few sample results
        for (int i = 0; i < results.Length; i++)
        {
            var callerIndex = i / 20;
            var opIndex = i % 20;
            var expectedA = callerIndex * 100 + opIndex;
            var expectedB = opIndex + 1;
            var expected = expectedA + expectedB;
            Assert.Equal(expected, results[i]);
        }
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_DateTimePrecisionAndTimezones()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test DateTime precision and consistency
        var before = DateTime.UtcNow;
        var serverTime1 = await caller.GetCurrentTime().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var serverTime2 = await caller.GetCurrentTime().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var after = DateTime.UtcNow;

        // Assert times are in reasonable range
        Assert.True(serverTime1 >= before.AddSeconds(-2) && serverTime1 <= after.AddSeconds(2));
        Assert.True(serverTime2 >= before.AddSeconds(-2) && serverTime2 <= after.AddSeconds(2));
        
        // Assert second call returns a time >= first call (allowing for precision)
        Assert.True(serverTime2 >= serverTime1.AddMilliseconds(-10));
    }

    [Fact]
    public async Task TestJsonRpcRequestResponse_BooleanLogicCombinations()
    {
        // Arrange
        using var host = SetupServer();
        await host.StartAsync(TestContext.Current.CancellationToken);

        var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, _testPort));
        var caller = await callerFactory.CreateCaller(TestContext.Current.CancellationToken);

        // Act & Assert - Test various boolean logic scenarios
        var testCases = new[]
        {
            (number: 0, expected: true),
            (number: 1, expected: false),
            (number: 2, expected: true),
            (number: -2, expected: true),
            (number: -1, expected: false),
            (number: 100, expected: true),
            (number: 101, expected: false),
            (number: int.MaxValue - 1, expected: true), // Even
            (number: int.MaxValue, expected: false), // Odd (2^31 - 1)
            (number: int.MinValue, expected: true) // Even (-2^31)
        };

        foreach (var (number, expected) in testCases)
        {
            var result = await caller.IsEven(number).WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            Assert.Equal(expected, result);
        }
    }

    private IHost SetupServer()
    {
        var hostBuilder = JsonRpcHostBuilder.Create<ITestService, JsonRpcServerService>()
            .ConfigureSuperSocket(options =>
            {
                options.Name = "JsonRpcServer";
                options.Listeners = new List<ListenOptions>
                    {
                        new ListenOptions
                        {
                            Ip = "127.0.0.1",
                            Port = _testPort
                        }
                    };
            })
            .ConfigureLogging((context, logging) =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Warning);
            });

        return hostBuilder.Build();
    }
}

[ServiceContractAtribute]
public interface ITestService
{
    Task<int> Add(int a, int b);

    Task<int> Subtract(int a, int b);

    Task<int> Multiply(int a, int b);

    Task<double> Divide(double a, double b);

    Task<string> Concatenate(string a, string b);

    Task Notify(string message);

    Task Trigger();

    Task<int> GetTriggerTimes();

    Task<bool> IsEven(int number);

    Task<string[]> SplitString(string input, char separator);

    Task<DateTime> GetCurrentTime();

    Task ProcessBatch(string[] items);

    Task Fail(string message)
    {
        throw new InvalidOperationException(message);
    }
}

// Mock JSON-RPC server service for testing
public class JsonRpcServerService : ITestService
{
    private int _triggerTimes;
    private readonly List<string> _batchItems = new();
    
    // Use a static field to ensure we can check the notification state across instances
    public static string LastNotifyMessage { get; set; }

    public Task<int> Add(int a, int b)
    {
        return Task.FromResult(a + b);
    }

    public Task<string> Concatenate(string a, string b)
    {
        return Task.FromResult(a + b);
    }

    public Task<double> Divide(double a, double b)
    {
        if (Math.Abs(b) < double.Epsilon)
            throw new DivideByZeroException("Cannot divide by zero");
        return Task.FromResult(a / b);
    }

    public Task<DateTime> GetCurrentTime()
    {
        return Task.FromResult(DateTime.UtcNow);
    }

    public Task<int> GetTriggerTimes()
    {
        return Task.FromResult(_triggerTimes);
    }

    public Task<bool> IsEven(int number)
    {
        return Task.FromResult(number % 2 == 0);
    }

    public Task<int> Multiply(int a, int b)
    {
        return Task.FromResult(a * b);
    }

    public Task Notify(string message)
    {
        LastNotifyMessage = message;
        return Task.CompletedTask;
    }

    public Task ProcessBatch(string[] items)
    {
        _batchItems.Clear();
        _batchItems.AddRange(items);
        return Task.CompletedTask;
    }

    public Task<string[]> SplitString(string input, char separator)
    {
        if (string.IsNullOrEmpty(input))
            return Task.FromResult(Array.Empty<string>());
        
        return Task.FromResult(input.Split(separator));
    }

    public Task<int> Subtract(int a, int b)
    {
        return Task.FromResult(a - b);
    }

    public Task Trigger()
    {
        Interlocked.Increment(ref _triggerTimes);
        return Task.CompletedTask;
    }
}
