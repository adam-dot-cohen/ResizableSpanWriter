# Overview
![]()
[![NuGet version (ResizableSpanWriter)](https://img.shields.io/badge/nuget-v1.0.1-blue?style=flat-square)](https://www.nuget.org/packages/ResizableSpanWriter/)

High-performance, low-allocation, heap-based convience type for constructing Span<T> and Memory<T> structures without specifying size - implementing IBufferWriter<T> / IMemoryOwner<T>.   Better performance and efficiently than alternatives such as MemoryStream, RecyclableMemoryStream and ArrayPoolBufferWriter (MS High Performance Toolkit).

# Example Usage
```csharp
// count of sample integers we'll append the writer
var cnt = 2000;

//1. INSTANTIATE
var writer = new ResizableSpanWriter<int>();

//2. Write single entries
for (int i = 0; i < cnt; i++)
{
	// write to ResizableSpanWriter
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

|                              Method | TotalCount |       Mean |     Error | Allocated |
|------------------------------------ |----------- |-----------:|----------:|----------:|
|      'Proposed ResizableSpanWriter' |        100 |   1.974 us | 0.0547 us |     992 B |
| 'High Perf Toolkit ArrayPoolWriter' |        100 |   2.201 us | 0.0577 us |    1104 B |
|         'MS RecyclableMemoryStream' |        100 |   5.800 us | 0.1931 us |     872 B |
|        'DotNext SparseBufferWriter' |        100 |   6.527 us | 0.1759 us |    1160 B |
|      'Proposed ResizableSpanWriter' |       1000 |   2.691 us | 0.0596 us |     992 B |
| 'High Perf Toolkit ArrayPoolWriter' |       1000 |   2.496 us | 0.0535 us |    2168 B |
|         'MS RecyclableMemoryStream' |       1000 |   6.464 us | 0.2420 us |     872 B |
|        'DotNext SparseBufferWriter' |       1000 |   7.991 us | 0.2731 us |    1160 B |
|      'Proposed ResizableSpanWriter' |      10000 |   6.160 us | 0.1251 us |     992 B |
| 'High Perf Toolkit ArrayPoolWriter' |      10000 |   6.444 us | 0.1349 us |   12872 B |
|         'MS RecyclableMemoryStream' |      10000 |  11.476 us | 0.2429 us |     872 B |
|        'DotNext SparseBufferWriter' |      10000 |  12.894 us | 0.2579 us |    1336 B |
|      'Proposed ResizableSpanWriter' |     100000 |  34.300 us | 0.4911 us |     992 B |
| 'High Perf Toolkit ArrayPoolWriter' |     100000 |  37.429 us | 0.7096 us |  119744 B |
|         'MS RecyclableMemoryStream' |     100000 |  57.658 us | 0.3517 us |     872 B |
|        'DotNext SparseBufferWriter' |     100000 |  56.562 us | 0.8860 us |    3272 B |
|      'Proposed ResizableSpanWriter' |    1000000 | 143.571 us | 2.7542 us |     992 B |
| 'High Perf Toolkit ArrayPoolWriter' |    1000000 | 178.754 us | 0.9023 us | 1188488 B |
|         'MS RecyclableMemoryStream' |    1000000 | 523.529 us | 3.2797 us |    1504 B |
|        'DotNext SparseBufferWriter' |    1000000 | 553.662 us | 5.8670 us |   22632 B |
```
