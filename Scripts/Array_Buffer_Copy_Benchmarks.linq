<Query Kind="Program">
  <Namespace>System.Buffers</Namespace>
</Query>

void Main()
{
}

static int chunkLength = 8;
static int[] arrayOfInts = Enumerable.Range(0,  1024).ToArray();
Memory<int> memOfInts = arrayOfInts;
ArrayPool<int> pool = ArrayPool<int>.Shared;

// PooledArrayBufferWriter
// Simulate Array.Copy() to copy / grow buffer
public void PooledArrayBufferWriter_Array_Copy()
{
	int[] arrayOfIntsDest = pool.Rent(1024);

	for (int x = 0; x < 1024 / chunkLength; x++)
	{
		var startIndex = x * chunkLength;
		Array.Copy(arrayOfInts, startIndex, arrayOfIntsDest, startIndex, chunkLength);
	}
}

// MemoryBufferyWriter (original)
// Simulate instance Memory<T> over T[] to grow / copy buffer
public void Orig_MemoryBufferyWriter_Memory_Copy()
{
	Memory<int> arrayOfIntsDest = pool.Rent(1024);

	for (int x = 0; x < 1024 / chunkLength; x++)
	{
		var startIndex = x * chunkLength;
		var src = new Span<int>(arrayOfInts, startIndex, chunkLength);
		var dst = arrayOfIntsDest.Span.Slice(startIndex, chunkLength);
		src.CopyTo(dst);
	}
}

// MemoryBufferWriter (modified)
// Simulate local Span<T> over T[] to copy / grow buffer
public void New_MemoryBufferyWriter_Span_CopyTo()
{
	var arrayOfIntsDest = pool.Rent(1024);

	for (int x = 0; x < 1024 / chunkLength; x++)
	{
		var startIndex = x * chunkLength;
		var src = new Span<int>(arrayOfInts, startIndex, chunkLength);
		var dst = new Span<int>(arrayOfIntsDest, startIndex, chunkLength);
		src.CopyTo(dst);
	}
}