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

namespace ResizableSpanWriter.Benchmarks;
[SimpleJob(runStrategy: RunStrategy.Throughput, launchCount: 1, invocationCount: 1, runtimeMoniker: RuntimeMoniker.Net70)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class ChampionChallengerBenchmarks
{
    private static readonly RecyclableMemoryStreamManager manager = new();
    private readonly byte[] chunk = new byte[128];

    [Params(100, 10_000, 100_000)]
    public int TotalCount;

    [Benchmark(Description = "ResizableSpanWriter")]
    public void ResizableSpanByte()
    {
        var i = sizeof(int);
        var writer = new ResizableSpanWriter<byte>();
        Write(writer);
    }
    [Benchmark(Description = "MemoryStream", Baseline = true)]
    public void WriteToMemoryStream()
    {
        using var ms = new MemoryStream();
        Write(ms);
    }

    [Benchmark(Description = "ArrayPoolBufferWriter")]
    public void ArrayPoolBufferWriterInternal()
    {
        using ArrayPoolBufferWriter<byte> writer = new();
        Write(writer);
    }
    
    [Benchmark(Description = "RecyclableMemoryStream")]
    public void WriteToRecyclableMemoryStream()
    {
        using var ms = manager.GetStream();
        Write(ms);
    }

    private void Write(Stream output)
    {
        for (int remaining = TotalCount, taken; remaining > 0; remaining -= taken)
        {
            taken = Math.Min(remaining, chunk.Length);
            output.Write(chunk, 0, taken);
        }
    }

    private unsafe void Write(ArrayPoolBufferWriter<byte> output)
    {
        for (int remaining = TotalCount, taken; remaining > 0; remaining -= taken)
        {
            taken = Math.Min(remaining, chunk.Length);
            var span = output.GetSpan(taken);
            chunk[..taken].CopyTo(span);
        }
    }
    private void Write(ResizableSpanWriter<byte> output)
    {
        for (int remaining = TotalCount, taken; remaining > 0; remaining -= taken)
        {
            taken = Math.Min(remaining, chunk.Length);
            output.Write(chunk);
        }
    }
}
