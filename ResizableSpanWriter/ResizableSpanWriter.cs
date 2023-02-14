using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ResizableSpanWriter;

/// <summary>
/// Represents a heap-based, array-backed output sink into which <typeparamref name="T"/> data can be written.
/// </summary>
/// <typeparam name="T">The type of items to write to the current instance.</typeparam>
/// <remarks>
public class ResizableSpanWriter<T>
{
    /// <summary>
    /// The default size to use to expand the buffer.
    /// </summary>
    private const int DefaultGrowthIncrement = 256;

    /// <summary>
    /// Array on current rental from the array pool.  Reference to the same memory as <see cref="_buffer"/>.
    /// </summary>
    private T[] _array;

    /// <summary>
    /// The increment to use to grow the writer.
    /// </summary>
    /// 
    private readonly int _growthIncrement;

    /// <summary>
    /// The <see cref="ArrayPool{T}"/> instance used to rent <see cref="array"/>.
    /// </summary>
    private Memory<T> _buffer;

    /// <summary>
    /// The current position of the writer.
    /// </summary>
    private int _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizableSpanWriter{T}"/> class.
    /// </summary>
    public ResizableSpanWriter()
        : this(ArrayPool<T>.Shared, DefaultGrowthIncrement)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResizableSpanWriter{T}"/> class.
    /// </summary>
    /// <param name="growthIncrement">The incremental size to grow the buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="growthIncrement"/> is not valid.</exception>
    public ResizableSpanWriter(int growthIncrement)
        : this(ArrayPool<T>.Shared, growthIncrement)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayPoolBufferWriter{T}"/> class.
    /// </summary>
    /// <param name="pool">The <see cref="ArrayPool{T}"/> instance to use.</param>
    /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="growthGrowthIncrement"/> is not valid.</exception>
    public ResizableSpanWriter(ArrayPool<T> pool, int growthGrowthIncrement)
    {
        if (growthGrowthIncrement < 1)
            throw new ArgumentOutOfRangeException("The growth increment parameter bust be greater than 0");

        this._buffer = _array = ArrayPool<T>.Shared.Rent(growthGrowthIncrement);
        this._growthIncrement = growthGrowthIncrement;
        this._index = 0;
    }

    /// <summary>
    /// Gets the data written to the underlying buffer so far as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<T> WrittenMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return this._buffer.Slice(0, _index);
        }
    }

    /// <summary>
    /// Gets the data written to the underlying buffer so far as a <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    public ReadOnlySpan<T> WrittenSpan
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return this._buffer.Span.Slice(0, _index);
        }
    }
    
    /// <summary>
    /// Appends to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="Span{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Span<T> items)
    {
        Copy(items);
    }

    /// <summary>
    /// Appends to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">Array of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T[] items)
    {
        Copy(items);
    }

    /// <summary>
    /// Appends to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="Memory{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Memory<T> items)
    {
        Copy(items.Span);
    }

    /// <summary>
    /// Appends a single item to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="item"> Item <see cref="T"/> to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T item)
    {
        Copy(item);
    }

    /// <summary>
    /// Appends to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">Pointer of items, which must be pinned/fixed.</param>
    /// <param name="length">The length of the pointer</param>
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //public unsafe void Write(T* items, int length)
    //{
    //   Grow(length);

    //    for (var i = 0; i < length; i++)
    //    {
    //        slice[i] = items[i];
    //    }
    //}

    /// <summary>
    /// Appends to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="Span{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteAsync(Memory<T> items)
    {
        Copy(items.Span);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Appends a single item to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="Span{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteAsync(T item)
    {
        Copy(item);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Grows the buffer if needed.
    /// </summary>
    /// <param name="length"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow(int length)
    {
        if (_index + length <= _buffer.Span.Length) return;

        var next = ArrayPool<T>.Shared.Rent(Math.Max(_index + _growthIncrement, _index + length));

        _buffer.Span.CopyTo(next);

        ArrayPool<T>.Shared.Return(_array);

        _buffer = _array = next;
    }

    /// <summary>
    /// Returns a slice of the underlying buffer
    /// </summary>
    /// <param name="length">The length of the desired slice.</param>
    /// <returns><see cref="Span{T}"/> of the underlying buffer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(Span<T> items)
    {
        Grow(items.Length);

        var slc = _buffer.Span.Slice(_index, items.Length);

        items.CopyTo(slc);

        _index += items.Length;
       
        //unsafe
        //{
        //    fixed (T* ptr = slc, ptrSrc = items)
        //    {
        //        Unsafe.CopyBlockUnaligned(ptr, ptrSrc, (uint)items.Length);
        //    }
        //}

    }
    /// <summary>
    /// Returns a slice of the underlying buffer
    /// </summary>
    /// <param name="length">The length of the desired slice.</param>
    /// <returns><see cref="Span{T}"/> of the underlying buffer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(T item)
    {
        Grow(1);

        var slc = _buffer.Span.Slice(_index, 1);

        _index += 1;

        slc[0] = item;
    }
}
