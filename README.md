# Overview
![]()
[![NuGet version (ResizeableSpanWriter)](https://img.shields.io/badge/nuget-v1.0.0blue?style=flat-square)](https://www.nuget.org/packages/ResizeableSpanWriter/)

If you're looking for the fastest binary serializer for DotNet known to Git-kind, look no further.  ResizeableSpanWriter is up to ***18x faster than [MessagePack](https://github.com/neuecc/MessagePack-CSharp) and [Protobuf](https://github.com/protocolbuffers/protobuf), and 11x faster than [BinaryPack](https://github.com/Sergio0694/BinaryPack)***, with roughly equivelant or better memory allocation. Simply install the [Nuget package (Install-Package ResizeableSpanWriter)](https://www.nuget.org/packages/ResizeableSpanWriter/) and serialize/deserialize with just 2 lines of code.

# Example Usage
```csharp
// count of sample integers we'll append the writer
	var cnt = 2000;
	
	// instantiate with desired type arguement
	var writer = new ResizableSpanWriter<int>();
	
	// normal span for illustrative pu
	Span<int> span = new int[cnt];
	
	for (int i = 0; i < cnt; i++)
	{
		// write to ResizableSpanWriter
		writer.Write(i);
		
		// write to Span
		span[i] = i;
	}
	
	// read from ResizableSpanWriter.WrittenSpan
	Console.WriteLine(writer.WrittenSpan.SequenceEqual(span));	
```
## Benchmarks
Benchmarks performed using BenchmarkDotNet follow the intended usage pattern of serializing and deserializing a single instance of an object at a time (as opposed to batch collection serialization used in the benchmarks published by other libraries such as Apex).  The benchmarks charts displayed below represent 1 million syncronous serialization and deserialization operations of the following object:

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
## ResizeableSpanWriterUnsafe\<T\>
The ResizeableSpanWriter project contains an unsafe implementation of the ResizeableSpanWriter\<T\>, named ResizeableSpanWriterUnsafe\<T\>.  It is an experimental version intended for benchmarking purposes and does not meaningfuly outperform ResizeableSpanWriter\<T\> in most scenarios, if at all (see table above).  As such, it is not recommended for end user consumption.

## Limitations 
### Unsupported types
Serialization of the following types and nested types is planned but not supported at this time (if you would like to contribute, fork away or reach out to collaborate)...

- Complex type properties (i.e. a class with a property of type ANY class).  If a class contains a property that is a complex type, the class will still be serialized but the property will be ignored.
- Dictionaries are not supported at this type (arrays, generic lists, etc. are supported). If a class contains a property of type Dictionary, the class will still be serialized but the property will be ignored.

### Property Exclusion
If you need to exclude a property from being serialized for reasons other then performance (unless nanoseconds actually matter to you), presently your only option is a DTO.  If you would like this feature added feel free to contribute or log an issue.
    
## Feedback, Suggestions and Contributions
Are all welcome!
