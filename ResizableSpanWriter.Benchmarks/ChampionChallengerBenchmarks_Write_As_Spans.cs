using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.IO;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Columns;
using DotNext.Buffers;
using DotNext.Collections.Generic;

namespace ResizableSpanWriter.Benchmarks;
[SimpleJob(runStrategy: RunStrategy.Throughput, launchCount: 1, invocationCount: 1, runtimeMoniker: RuntimeMoniker.Net70)]

[HideColumns(Column.StdDev, Column.Median, Column.RatioSD)]
[MemoryDiagnoser]
public class ChampionChallengerBenchmarks_Write_As_Spans
{
    private static readonly RecyclableMemoryStreamManager manager = new();
    private readonly byte[] chunk = new byte[128];

    [Params(100, 1_000, 10_000, 100_000, 1_000_000)]
    public int TotalCount;

    [Benchmark(Description = "Proposed MemoryBufferWriter", Baseline = true)]
    public void SpanBufferWriter()
    {
        using var writer = new System.Buffers.MemoryBufferWriter<byte>();
		this.Write(writer);
    }

    [Benchmark(Description = "High Perf Toolkit ArrayPoolWriter")]
    public void ArrayPoolBufferWriterInternal()
    {
        using ArrayPoolBufferWriter<byte> writer = new();
		this.Write(writer);
    }

	
    [Benchmark(Description = "DotNext PooledBufferWriter")]
    public void SparseBufferWriter()
    {
	    var ms = new PooledBufferWriter<byte>();
	    this.Write(ms);
    }

    [Benchmark(Description = "MS RecyclableMemoryStream")]
    public void WriteToRecyclableMemoryStream()
    {
        using var ms = manager.GetStream();
		this.Write(ms);
    }

    private void Write(Stream output)
    {
		
	    Span<byte> bytes = stackalloc byte[this.chunk.Length];
		bytes = this.chunk;
	    for (int remaining = this.TotalCount, taken; remaining > 0; remaining -= taken)
	    {
            taken = Math.Min(remaining, this.chunk.Length);
            output.Write(bytes[taken..]);
        }
    }

    private unsafe void Write(ArrayPoolBufferWriter<byte> output)
    {  
	    Span<byte> bytes = stackalloc byte[this.chunk.Length];
	    bytes = this.chunk;
        for (int remaining = this.TotalCount, taken; remaining > 0; remaining -= taken)
        {
            taken = Math.Min(remaining, this.chunk.Length);
            var span = output.GetSpan(taken);
			bytes[taken..].CopyTo(span);
			output.Advance(taken);
        }
    }

    private void Write(System.Buffers.MemoryBufferWriter<byte> output)
    {
	    Span<byte> bytes = stackalloc byte[this.chunk.Length];
	    bytes = this.chunk;
        for (int remaining = this.TotalCount, taken; remaining > 0; remaining -= taken)
        {
            taken = Math.Min(remaining, this.chunk.Length);
            output.Write(bytes[taken..]);
        }
    }

    private void Write(PooledBufferWriter<byte> output)
    {
	    Span<byte> bytes = stackalloc byte[this.chunk.Length];
	    bytes = this.chunk;
	    for (int remaining = this.TotalCount, taken; remaining > 0; remaining -= taken)
	    {
		    taken = Math.Min(remaining, this.chunk.Length);
			output.Write(bytes[taken..]);
	    }
    }
}
