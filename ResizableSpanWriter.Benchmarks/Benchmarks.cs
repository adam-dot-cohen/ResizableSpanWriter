using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using CommunityToolkit.HighPerformance.Buffers;
using Microsoft.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Columns;
using DotNext.Buffers;

namespace ResizableSpanWriter.Benchmarks;
[SimpleJob(runStrategy: RunStrategy.Throughput, launchCount: 1, invocationCount: 1, runtimeMoniker: RuntimeMoniker.Net70)]

[HideColumns(Column.StdDev, Column.Median, Column.RatioSD)]
[MemoryDiagnoser]
public class ChampionChallengerBenchmarks
{
    private static readonly RecyclableMemoryStreamManager manager = new();
    private readonly byte[] chunk = new byte[128];

    [Params(100, 1_000, 10_000, 100_000, 1_000_000)]
    public int TotalCount;

    [Benchmark(Description = "Proposed ResizableSpanWriter")]
    public void ResizableSpanByte()
    {
        var i = sizeof(int);
        using var writer = new ResizableSpanWriter<byte>();
		this.Write(writer);
    }

    [Benchmark(Description = "High Perf Toolkit ArrayPoolWriter")]
    public void ArrayPoolBufferWriterInternal()
    {
        using ArrayPoolBufferWriter<byte> writer = new();
		this.Write(writer);
    }
    
    [Benchmark(Description = "MS RecyclableMemoryStream")]
    public void WriteToRecyclableMemoryStream()
    {
        using var ms = manager.GetStream();
		this.Write(ms);
    }
    [Benchmark(Description = "DotNext SparseBufferWriter")]
    public void SparseBufferWriter()
    {
	    using var ms = new SparseBufferWriter<byte>();
	    this.Write(ms);
    }
    private void Write(Stream output)
    {
        for (int remaining = this.TotalCount, taken; remaining > 0; remaining -= taken)
        {
            taken = Math.Min(remaining, this.chunk.Length);
            output.Write(this.chunk, 0, taken);
        }
    }

    private unsafe void Write(ArrayPoolBufferWriter<byte> output)
    {
        for (int remaining = this.TotalCount, taken; remaining > 0; remaining -= taken)
        {
            taken = Math.Min(remaining, this.chunk.Length);
            var span = output.GetSpan(taken);
			this.chunk[..taken].CopyTo(span);
        }
    }
    private void Write(ResizableSpanWriter<byte> output)
    {
        for (int remaining = this.TotalCount, taken; remaining > 0; remaining -= taken)
        {
            taken = Math.Min(remaining, this.chunk.Length);
            output.Write(this.chunk);
        }
    }
    private void Write(SparseBufferWriter<byte> output)
    {
	    for (int remaining = this.TotalCount, taken; remaining > 0; remaining -= taken)
	    {
		    taken = Math.Min(remaining, this.chunk.Length);
		    output.Write(this.chunk);
	    }
    }
}
