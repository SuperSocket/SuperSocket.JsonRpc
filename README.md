# SuperSocket.JsonRpc

A comprehensive JSON-RPC 2.0 implementation built on top of SuperSocket, providing high-performance remote procedure call capabilities with full type conversion support and robust error handling.

## Features

- **JSON-RPC 2.0 Compliant**: Full implementation of the JSON-RPC 2.0 specification
- **SuperSocket Integration**: Leverages SuperSocket for high-performance networking
- **Type Safety**: Comprehensive type conversion system supporting primitives, arrays, and complex types
- **Pipeline Filters**: Custom pipeline filters for request/response processing
- **Client Library**: Dedicated client library (`SuperSocket.JsonRpc.Caller`) for easy integration
- **Error Handling**: Robust error propagation and exception handling
- **Concurrent Processing**: Support for parallel request handling
- **Notification Support**: Fire-and-forget notification messages
- **Unicode Support**: Full Unicode and special character handling

## Architecture

### Core Components

- **JsonElementExpressionConverter**: Advanced type conversion system supporting:
  - All primitive types (int, string, bool, char, double, DateTime)
  - Array types with recursive element conversion
  - Null value handling
  - Edge case number handling (int.MinValue, int.MaxValue)

- **Pipeline Filters**:
  - `JsonRpcRequestPipelineFilter`: Server-side request processing
  - `JsonRpcResponsePipelineFilter`: Client-side response processing

- **Client Library**: `SuperSocket.JsonRpc.Caller` provides easy-to-use client interface

### Projects Structure

```
SuperSocket.JsonRPC/
├── src/
│   ├── SuperSocket.JsonRpc/           # Core JSON-RPC implementation
│   └── SuperSocket.JsonRpc.Caller/    # Client library
└── test/
    └── SuperSocket.JsonRpc.Tests/     # Comprehensive test suite
```

## Quick Start

### Server Setup

```csharp
// Configure your JSON-RPC service
public interface ITestService
{
    Task<int> Add(int a, int b);
    Task<string> Echo(string message);
    Task NotifyAsync(string message);
}

// Implementation
public class TestService : ITestService
{
    public async Task<int> Add(int a, int b) => a + b;
    public async Task<string> Echo(string message) => message;
    public async Task NotifyAsync(string message) { /* handle notification */ }
}
```

### Client Usage

```csharp
// Create a caller factory with the server endpoint
var callerFactory = new JsonRpcCallerFactory<ITestService>(new IPEndPoint(IPAddress.Loopback, 4040));

// Create a type-safe caller
var caller = await callerFactory.CreateCaller();

// Make remote calls with full type safety
var result = await caller.Add(5, 3);
// result = 8

// Other examples
var message = await caller.Echo("Hello, JSON-RPC!");
await caller.NotifyAsync("This is a notification");
```

## Testing

The project includes a comprehensive test suite with **25 test scenarios** covering:

### Core Functionality (17 tests)
- Basic request/response operations
- Mathematical operations (Add, Subtract, Multiply, Divide)
- String operations and concatenation
- Boolean logic (IsEven)
- Array processing (SplitString)
- DateTime operations
- Error handling and exception propagation
- Notification handling
- State management across requests
- Connection recovery scenarios

### Advanced Scenarios (8 tests)
- **Null and Empty Values**: Validation of null/empty string handling and negative numbers
- **Edge Case Numbers**: Testing int.MinValue, int.MaxValue, and floating-point precision
- **Unicode and Special Characters**: Full Unicode emoji support and special character handling
- **Large Array Processing**: 1000-item arrays and 500-item batch processing
- **Performance Comparison**: Sequential vs concurrent processing benchmarks
- **Connection Stability**: 5 concurrent connections with 20 operations each (100 total operations)
- **DateTime Precision**: DateTime consistency and timezone handling
- **Boolean Logic Edge Cases**: Complex boolean operations with edge case numbers

### Performance Characteristics
- **Concurrent Processing**: Supports 10+ parallel requests
- **Large Data Handling**: Tested with 1000+ item arrays
- **Connection Stability**: Validated under high load (100+ concurrent operations)
- **Memory Efficiency**: Optimized type conversion and object pooling

## Type Support

The framework supports comprehensive type conversion including:

- **Primitives**: `int`, `string`, `bool`, `char`, `double`, `DateTime`
- **Arrays**: `string[]`, `int[]`, and other primitive arrays
- **Null Values**: Proper null handling for nullable types
- **Edge Cases**: Special number handling (MinValue, MaxValue, infinity)
- **Unicode**: Full Unicode character support including emojis

## Error Handling

- JSON-RPC 2.0 compliant error responses
- Exception propagation from server to client
- Proper error codes and messages
- Connection recovery mechanisms

## Performance

The framework is designed for high performance with:
- Efficient JSON serialization/deserialization
- Minimal memory allocations
- Connection pooling support
- Concurrent request processing

## Requirements

- .NET Core/Framework compatible
- SuperSocket dependencies
- Newtonsoft.Json for JSON processing

## Contributing

This project maintains high code quality with comprehensive test coverage. All changes should include appropriate tests and maintain the existing test pass rate.
