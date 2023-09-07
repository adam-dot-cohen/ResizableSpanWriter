# Overview
![]()
[![NuGet version (ResizableSpanWriter)](https://img.shields.io/badge/nuget-v1.0.1-blue?style=flat-square)](https://www.nuget.org/packages/ResizableSpanWriter/)

High-performance, low-allocation, heap-based convience type for constructing Span<T> and Memory<T> structures without specifying size - implementing IBufferWriter<T> / IMemoryOwner<T>.   Better performance and efficiently than alternatives such as MemoryStream, RecyclableMemoryStream and ArrayPoolBufferWriter (MS High Performance Toolkit).

# Example Usage
```csharp
// count of sample integers we'll append the writer
var cnt = 2000;

//1. INSTANTIATE
var writer = new SpanBufferWriter<int>();

//2. Write single entries
for (int i = 0; i < cnt; i++)
{
	// write to SpanBufferWriter
	writer.Write(i);
}

//3. Write array, span or memory...
writer.Write(span);

//4. Read contents from - `WrittenSpan` OR `WrittenMemory`
Console.WriteLine(writer.WrittenSpan.SequenceEqual(span));
```
## Benchmarks
Benchmarks performed using BenchmarkDotNet...

```
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1415 (21H2)
Intel Core i9-10980XE CPU 3.00GHz, 1 CPU, 36 logical and 18 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 7.0.1 (6.0.121.56705), X64 RyuJIT
  Job-XTVWKK : .NET 7.0.1 (6.0.121.56705), X64 RyuJIT

|                              Method | TotalCount |       Mean |      Error | Allocated |
|------------------------------------ |----------- |-----------:|-----------:|----------:|
|         'Proposed SpanBufferWriter' |        100 |   2.245 us |  0.1280 us |     992 B |
| 'High Perf Toolkit ArrayPoolWriter' |        100 |   2.397 us |  0.1303 us |    1104 B |
|                         'MS Stream' |        100 |   2.127 us |  0.1010 us |     944 B |
|         'MS RecyclableMemoryStream' |        100 |   5.812 us |  0.2283 us |     872 B |
|        'DotNext SparseBufferWriter' |        100 |   6.462 us |  0.2818 us |    4944 B |
|         'Proposed SpanBufferWriter' |       1000 |   2.933 us |  0.1332 us |     992 B |
| 'High Perf Toolkit ArrayPoolWriter' |       1000 |   2.866 us |  0.1295 us |    2168 B |
|                         'MS Stream' |       1000 |   2.450 us |  0.1315 us |    2528 B |
|         'MS RecyclableMemoryStream' |       1000 |   6.870 us |  0.2566 us |     872 B |
|        'DotNext SparseBufferWriter' |       1000 |   7.088 us |  0.2757 us |    4944 B |
|         'Proposed SpanBufferWriter' |      10000 |   6.397 us |  0.2110 us |     992 B |
| 'High Perf Toolkit ArrayPoolWriter' |      10000 |   6.539 us |  0.1647 us |   12872 B |
|                         'MS Stream' |      10000 |   5.838 us |  0.2019 us |   33344 B |
|         'MS RecyclableMemoryStream' |      10000 |  11.617 us |  0.3087 us |     872 B |
|        'DotNext SparseBufferWriter' |      10000 |  12.560 us |  0.3530 us |   13360 B |
|         'Proposed SpanBufferWriter' |     100000 |  35.873 us |  1.0234 us |     992 B |
| 'High Perf Toolkit ArrayPoolWriter' |     100000 |  39.679 us |  0.7934 us |  119744 B |
|                         'MS Stream' |     100000 |  31.985 us |  1.6602 us |  262792 B |
|         'MS RecyclableMemoryStream' |     100000 |  59.855 us |  1.1866 us |     872 B |
|        'DotNext SparseBufferWriter' |     100000 |  57.376 us |  1.1503 us |  105936 B |
|         'Proposed SpanBufferWriter' |    1000000 | 147.532 us |  3.0570 us |     992 B |
| 'High Perf Toolkit ArrayPoolWriter' |    1000000 | 181.525 us |  2.5005 us | 1188488 B |
|                         'MS Stream' |    1000000 | 619.653 us | 21.9352 us | 2097872 B |
|         'MS RecyclableMemoryStream' |    1000000 | 521.721 us |  3.4314 us |    1504 B |
|        'DotNext SparseBufferWriter' |    1000000 | 472.577 us |  7.5930 us | 1031696 B |
```
