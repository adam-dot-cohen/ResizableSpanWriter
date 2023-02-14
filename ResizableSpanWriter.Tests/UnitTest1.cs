namespace ResizableSpanWriter.Tests;

public class ResizableSpanWriterTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Single_item_write_sequence_should_be_equal_to_span()
    {
        var cnt = 1000;
        var writer = new ResizableSpanWriter<int>(5);
        Span<int> shouldEqual = new int[cnt];
	
        for (int i = 0; i < cnt; i++)
        {
            writer.Write(i);
            shouldEqual[i] = i;
        }

        Assert.True(writer.WrittenSpan.SequenceEqual(shouldEqual));
    }

    [Test]
    public void Multiple_item_write_sequence_should_be_equal_to_span()
    {
        var cnt = 1000;
        var writer = new ResizableSpanWriter<int>(5);
        Span<int> shouldEqual = new int[cnt];
	
        for (int i = 0; i < cnt; i++)
        {
            var ii = new int[1] { i };
            var slc = shouldEqual.Slice(i, 1);
            ii.CopyTo(slc);

            writer.Write(ii);
        }

        Assert.True(writer.WrittenSpan.SequenceEqual(shouldEqual));
    }
}