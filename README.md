# Overview
![]()
[![NuGet version (ResizableSpanWriter)](https://img.shields.io/badge/nuget-v1.0.1-blue?style=flat-square)](https://www.nuget.org/packages/ResizableSpanWriter/)

High-performance writer for creating Span<T> and Memory<T> structures that outperforms MemoryStream, RecyclableMemoryStream and ArrayPoolBufferWriter (MS High Performance Toolkit).

# Example Usage
```csharp
// count of sample integers we'll append the writer
var cnt = 2000;

//1. INSTANTIATE
var writer = new ResizableSpanWriter<int>();

// normal span for illustrative purposes and checksum below
Span<int> span = new int[cnt];

//2. WRITE SINGLE ENTRIES - SEE #4 BELOW FOR ARRAYS...
for (int i = 0; i < cnt; i++)
{
	// write to ResizableSpanWriter
	writer.Write(i);

	// write to Span
	span[i] = i;
}

//3. READ FROM - WRITTENSPAN OR WRITTENMEMORY
Console.WriteLine(writer.WrittenSpan.SequenceEqual(span));

//4. ALTERNATIVELIY - WRITE ARRAYS, SPANS, MEMORY
writer.Write(span);
```
## Benchmarks
Benchmarks performed using BenchmarkDotNet...

```
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1415 (21H2)
Intel Core i9-10980XE CPU 3.00GHz, 1 CPU, 36 logical and 18 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  Job-XTVWKK : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT

|                 Method | TotalCount |      Mean |     Error | Ratio |
|----------------------- |----------- |----------:|----------:|------:|
|           MemoryStream |        100 |  2.452 us | 0.0497 us |  1.00 |
|    ResizableSpanWriter |        100 |  2.497 us | 0.0558 us |  1.02 |
|  ArrayPoolBufferWriter |        100 |  2.888 us | 0.0623 us |  1.19 |
| RecyclableMemoryStream |        100 |  6.735 us | 0.1385 us |  2.79 |
|                        |            |           |           |       |
|    ResizableSpanWriter |      10000 |  5.981 us | 0.1250 us |  0.96 |
|           MemoryStream |      10000 |  6.429 us | 0.3840 us |  1.00 |
|  ArrayPoolBufferWriter |      10000 |  6.890 us | 0.1348 us |  1.10 |
| RecyclableMemoryStream |      10000 | 12.671 us | 0.2534 us |  2.00 |
|                        |            |           |           |       |
|    ResizableSpanWriter |     100000 | 31.933 us | 0.6427 us |  0.90 |
|           MemoryStream |     100000 | 35.572 us | 0.7141 us |  1.00 |
|  ArrayPoolBufferWriter |     100000 | 41.672 us | 0.8295 us |  1.18 |
| RecyclableMemoryStream |     100000 | 60.085 us | 0.6064 us |  1.69 |
```
    
## Feedback, Suggestions and Contributions
Are all welcome!
