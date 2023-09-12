using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers;

/// <summary>
/// Represents a heap-based, array-backed output sink into which <typeparamref name="T"/> data can be written.
/// </summary>
/// <typeparam name="T">The type of items to write to the current instance.</typeparam>
/// <remarks>
public class MemoryBufferWriter<T> : IBufferWriter<T>, IMemoryOwner<T>
{
    /// <summary>
    /// The default size to use to expand the buffer.
    /// </summary>
    private const int DefaultGrowthIncrement = 512;

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
    /// The disposed state of the buffer.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryBufferWriter{T}"/> class.
    /// </summary>
    public MemoryBufferWriter()
        : this(ArrayPool<T>.Shared, DefaultGrowthIncrement)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryBufferWriter{T}"/> class.
    /// </summary>
    /// <param name="growthIncrement">The incremental size to grow the buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="growthIncrement"/> is not valid.</exception>
    public MemoryBufferWriter(int growthIncrement)
        : this(ArrayPool<T>.Shared, growthIncrement)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryBufferWriter{T}"/> class.
    /// </summary>
    /// <param name="pool">The <see cref="ArrayPool{T}"/> instance to use.</param>
    /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="growthGrowthIncrement"/> is not valid.</exception>
    public MemoryBufferWriter(ArrayPool<T> pool, int growthGrowthIncrement)
    {
        if (growthGrowthIncrement < 1)
            throw new ArgumentOutOfRangeException("The growth increment parameter bust be greater than 0");

        this._buffer = this._array = ArrayPool<T>.Shared.Rent(growthGrowthIncrement);
        this._growthIncrement = growthGrowthIncrement;
        this._index = 0;
        this._disposed = false;
    }

    /// <summary>
    /// Gets the data written to the underlying buffer so far as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<T> WrittenMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            return this._buffer.Slice(0, this._index);
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
            return this._buffer.Span.Slice(0, this._index);
        }
    }

    /// <inheritdoc />
    Memory<T> IMemoryOwner<T>.Memory => this._buffer;
    
    /// <inheritdoc />
    public void Advance(int count)
    {
        if(this._index + count <= this._buffer.Span.Length)
			this.Grow(count);

		this._index += count;
    }

    /// <inheritdoc />
    public Memory<T> GetMemory(int sizeHint = 0)
    {
        if (sizeHint == 0)
            sizeHint = 8;

		this.Grow(sizeHint);

        var slcIndex = this._index;

		this._index += sizeHint;

        return this._buffer.Slice(slcIndex, sizeHint);
    }

    /// <inheritdoc />
    public Span<T> GetSpan(int sizeHint = 0)
    {
        if (sizeHint == 0)
            sizeHint = 8;

		this.Grow(sizeHint);

        var slcIndex = this._index;

		this._index += sizeHint;

        return this._buffer.Span.Slice(slcIndex, sizeHint);
    }

    /// <summary>
    /// Appends to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="Span{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Span<T> items)
        => this.Copy(items);

    /// <summary>
    /// Appends to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">Array of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T[] items)
        => this.Copy(items);

    /// <summary>
    /// Appends to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="Memory{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Memory<T> items)
        => this.Copy(items.Span);

    /// <summary>
    /// Appends a single item to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="item"> Item <see cref="T"/> to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T item)
        => this.Copy(item);

    /// <summary>
    /// Appends to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="Span{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteAsync(Memory<T> items)
    {
		this.Copy(items.Span);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Appends a single item to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="Span{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ValueTask WriteAsync(T item)
    {
		this.Copy(item);

        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Grows the buffer if needed.
    /// </summary>
    /// <param name="length"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow(int length)
    {
        if (this._index + length <= this._buffer.Span.Length) return;

        var next = ArrayPool<T>.Shared.Rent(Math.Max(this._index + this._growthIncrement, this._index + length));

		this._buffer.Span.CopyTo(next);

        ArrayPool<T>.Shared.Return(this._array);

		this._buffer = this._array = next;
    }

    /// <summary>
    /// Returns a slice of the underlying buffer
    /// </summary>
    /// <param name="length">The length of the desired slice.</param>
    /// <returns><see cref="Span{T}"/> of the underlying buffer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(Span<T> items)
    {
		this.Grow(items.Length);

        var slc = this._buffer.Span.Slice(this._index, items.Length);

        items.CopyTo(slc);

		this._index += items.Length;

    }
    /// <summary>
    /// Returns a slice of the underlying buffer
    /// </summary>
    /// <param name="length">The length of the desired slice.</param>
    /// <returns><see cref="Span{T}"/> of the underlying buffer.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Copy(T item)
    {
		this.Grow(1);

        var slc = this._buffer.Span.Slice(this._index, 1);

        slc[0] = item;

		this._index += 1;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(this._array);

		this._buffer = null;
    }
}