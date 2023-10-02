using System.Buffers;

namespace ResizableSpanWriter.Tests;

public class ResizableSpanWriterTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Should_write_single_items_equal_to_span()
    {
        var cnt = 1000;
        var writer = new MemoryBufferWriter<int>(5);
        Span<int> shouldEqual = new int[cnt];
	
        for (int i = 0; i < cnt; i++)
        {
            writer.Write(i);
            shouldEqual[i] = i;
        }

        Assert.True(writer.WrittenSpan.SequenceEqual(shouldEqual));
    }

    [Test]
    public void Should_write_array_sequences_equal_to_span()
    {
        var cnt = 1000;
        var writer = new MemoryBufferWriter<int>(5);
        Span<int> shouldEqual = new int[cnt];
	
        for (int i = 0; i < cnt; i++)
        {
            var ii = new int[1] { i };
            var slc = shouldEqual.Slice(i, 1);
            ii.CopyTo(slc);
            Span<int> iii = ii;
            writer.Write(iii);
        }

        Assert.True(writer.WrittenSpan.SequenceEqual(shouldEqual));
    }

    [Test]
    public void Should_write_span_sequences_equal_to_span()
    {
        var cnt = 1000;
        var writer = new MemoryBufferWriter<int>(5);
        Span<int> shouldEqual = new int[cnt];
	
        for (int i = 0; i < cnt; i++)
        {
            Span<int> ii = new int[1] { i };
            var slc = shouldEqual.Slice(i, 1);
            ii.CopyTo(slc);

            writer.Write(ii);
        }

        Assert.True(writer.WrittenSpan.SequenceEqual(shouldEqual));
    }

    [Test]
    public void Should_write_memory_sequences_equal_to_span()
    {
        var cnt = 1000;
        var writer = new MemoryBufferWriter<int>(5);
        Span<int> shouldEqual = new int[cnt];
	
        for (int i = 0; i < cnt; i++)
        {
            Memory<int> ii = new int[1] { i };
            
            var slc = shouldEqual.Slice(i, 1);
            
            ii.Span.CopyTo(slc);

            writer.Write(ii);
        }
        Assert.True(writer.WrittenSpan.SequenceEqual(shouldEqual));
    }
    [Test]
    public void Should_advance_equal_to_span()
    {
        var cnt = 1000;
        var writer = new MemoryBufferWriter<int>(5);
        Span<int> shouldEqual = new int[cnt];

        int skipIndex = 0,skipCount = 10;

        for (int i = 0; i < cnt; i++)
        {
            Memory<int> ii = new int[1] { i };

            var slc = shouldEqual.Slice(i, 1);

            if (!(skipCount + skipIndex > i && skipIndex <= i))
            {
                ii.Span.CopyTo(slc);
                writer.Write(ii);
            }
            if(skipIndex == i)
                writer.Advance(skipCount);
        }
        Assert.True(writer.WrittenSpan.SequenceEqual(shouldEqual));
    }
}