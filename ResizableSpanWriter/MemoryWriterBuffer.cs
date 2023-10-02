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
    private const int DefaultGrowthMultiple = 2;

    /// <summary>
    /// Array on current rental from the array pool.  Reference to the same memory as <see cref="_buffer"/>.
    /// </summary>
    private T[] _array;

    /// <summary>
    /// The <see cref="ArrayPool{T}"/> instance used to rent <see cref="array"/>.
    /// </summary>
    private ArrayPool<T> _pool;

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
        : this(ArrayPool<T>.Shared)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryBufferWriter{T}"/> class.
    /// </summary>
    /// <param name="initialCapacity">The incremental size to grow the buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity"/> is not valid.</exception>
    public MemoryBufferWriter(int initialCapacity = 1)
        : this(ArrayPool<T>.Shared, initialCapacity)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryBufferWriter{T}"/> class.
    /// </summary>
    /// <param name="pool">The <see cref="ArrayPool{T}"/> instance to use.</param>
    /// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity"/> is not valid.</exception>
    public MemoryBufferWriter(ArrayPool<T> pool, int initialCapacity = 1)
    {
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException("The growth increment parameter bust be greater than 0");

		this._pool = pool;
        this._index = 0;
        this._disposed = false;

        if (initialCapacity > 0)
        {
	        this._buffer = this._array = this._pool.Rent(initialCapacity);
        }

        new Memory<T>(this._array);
    }

    /// <summary>
    /// Gets the data written to the underlying buffer so far as a <see cref="ReadOnlyMemory{T}"/>.
    /// </summary>
    public ReadOnlyMemory<T> WrittenMemory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
			this.ThrowIfDisposed();

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
	        this.ThrowIfDisposed();

            return this._buffer.Span.Slice(0, this._index);
        }
    }

    /// <inheritdoc />
    Memory<T> IMemoryOwner<T>.Memory => this._buffer;
    
    /// <inheritdoc />
    public void Advance(int count)
    {
	    this.ThrowIfDisposed();

        if(checked(this._index + count) <= this._buffer.Span.Length)
			this.Grow(count);

		this._index += count;
    }

    /// <inheritdoc />
    public Memory<T> GetMemory(int sizeHint = 0)
    {
	    this.ThrowIfDisposed();

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
	    this.ThrowIfDisposed();

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
    /// <param name="items">A <see cref="Memory{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(Memory<T> items)
        => this.Copy(items.Span);

    /// <summary>
    /// Appends to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="items">A <see cref="Memory{T}"/> of items to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T[] items)
    {
	    unsafe
	    {
		    fixed (T* item = items)
			    
			    this.Copy(new Span<T>(item, items.Length));
		    
	    }
    }

    /// <summary>
    /// Appends a single item to the end of the buffer, automatically growing the buffer if necessary.
    /// </summary>
    /// <param name="item"> Item <see cref="T"/> to append.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(T item)
        => this.Copy(item);

    /// <summary>
    /// Grows the buffer if needed.
    /// </summary>
    /// <param name="length"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Grow(int length)
    {
	    this.ThrowIfDisposed();

		var indexAfter = checked(this._index + length);

	    if (indexAfter <= this._buffer.Span.Length) return;

	    T[] next = this._pool.Rent(this.GetNewCapacity(indexAfter));

		this._buffer.Span.CopyTo(next);

        this._pool.Return(this._array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());

		this._buffer = this._array = next;
    }

    /// <summary>
    /// Gets the length to growth the buffer
    /// </summary>
    /// <param name="length"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetNewCapacity(int length)
    {
	    var optimal = (long)this._buffer.Span.Length * DefaultGrowthMultiple;

	    if (optimal > int.MaxValue) optimal = int.MaxValue;

	    return (int) Math.Max(optimal, length);
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
	    if (this._disposed) return;

	    if (this._array != null)
	    {
		    this._pool.Return(this._array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
	    }

	    this._buffer = null;
    }

    public void ThrowIfDisposed()
    {
	    if (this._disposed) throw new ObjectDisposedException("The buffer has been disposed.");
    }
}