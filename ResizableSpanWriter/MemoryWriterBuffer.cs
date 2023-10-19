using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
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
	private T[]? _array;

	/// <summary>
	/// The <see cref="ArrayPool{T}"/> instance used to rent <see cref="array"/>.
	/// </summary>
	private ArrayPool<T> _pool;

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
	public MemoryBufferWriter(int initialCapacity = 0)
		: this(ArrayPool<T>.Shared, initialCapacity)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MemoryBufferWriter{T}"/> class.
	/// </summary>
	/// <param name="pool">The <see cref="ArrayPool{T}"/> instance to use.</param>
	/// <param name="initialCapacity">The minimum capacity with which to initialize the underlying buffer.</param>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialCapacity"/> is not valid.</exception>
	public MemoryBufferWriter(ArrayPool<T> pool, int initialCapacity = 0)
	{
		if (initialCapacity < 0)
			throw new ArgumentOutOfRangeException("The growth increment parameter bust be greater than 0");

		this._pool = pool;

		this._index = 0;

		this._disposed = false;

		this._array = initialCapacity == 0 ? Array.Empty<T>() : pool.Rent(initialCapacity);
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

			return new ReadOnlyMemory<T>(this._array, 0, this._index);
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

			return new Span<T>(this._array, 0, this._index);
		}
	}

	/// <inheritdoc />
	Memory<T> IMemoryOwner<T>.Memory
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			this.ThrowIfDisposed();

			return new Memory<T>(this._array);
		}
	}

	/// <inheritdoc />
	public void Advance(int count)
	{
		var newIndex = checked(this._array.Length + count);

		if (newIndex < 0)
			throw new ArgumentOutOfRangeException(null, nameof(count));

		if (this._index > this._array.Length - count)
			throw new InvalidOperationException("Attempt to advance beyond the length of the buffer.");

		this._index = newIndex;
	}

	/// <inheritdoc />
	public Memory<T> GetMemory(int sizeHint = 0)
	{
		if (sizeHint == 0) sizeHint = 8;

		this.Grow(sizeHint);

		return new Memory<T>(this._array, this._index, sizeHint);
	}

	/// <inheritdoc />
	public Span<T> GetSpan(int sizeHint = 0)
	{
		if (sizeHint == 0) sizeHint = 8;

		this.Grow(sizeHint);

		return new Span<T>(this._array, this._index, sizeHint);
	}

	/// <summary>
	/// Appends to the end of the buffer, advances the buffer and automatically growing the buffer if necessary.
	/// </summary>
	/// <param name="items">A <see cref="Span{T}"/> of items to append.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Write(Span<T> items)
		=> this.Copy(items);

	/// <summary>
	/// Appends to the end of the buffer, advances the buffer and automatically growing the buffer if necessary.
	/// </summary>
	/// <param name="items">A <see cref="Memory{T}"/> of items to append.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Write(Memory<T> items)
		=> this.Copy(items.Span);

	/// <summary>
	/// Appends to the end of the buffer, advances the buffer and automatically growing the buffer if necessary.
	/// </summary>
	/// <param name="items">A <see cref="Memory{T}"/> of items to append.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Write(T[] items)
		=> this.Copy(items);

	/// <summary>
	/// Appends a single item to the end of the buffer, automatically growing the buffer if necessary.
	/// </summary>
	/// <param name="item"> Item <see cref="T"/> to append.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void Write(T item)
	{
		this.Grow(1);

		this._array[this._index] = item;

		this._index += 1;
	}

	/// <summary>
	/// Copies to the underlying buffer
	/// </summary>
	/// <param name="items">Items to add.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Copy(Span<T> items)
	{
		this.Grow(items.Length);

		var dst = new Span<T>(this._array, this._index, items.Length);

		items.CopyTo(dst);

		this._index += items.Length;
	}

	/// <summary>
	/// Grows the buffer if needed.
	/// </summary>
	/// <param name="length"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Grow(int size)
	{
		this.ThrowIfDisposed();

		if (!this.GrowIfRequired(size, out var length)) return;

		var next = this._pool.Rent(length);

		var dst = new Span<T>(next, 0, this._index);

		var src = new Span<T>(this._array, 0, this._index);

		src.CopyTo(dst);

		this._pool.Return(this._array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());

		this._array = next;
	}

	/// <summary>
	/// Gets the length to growth the buffer
	/// </summary>
	/// <param name="length"></param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private bool GrowIfRequired(int size, out int length)
	{
		length = default;

		var newIndex = checked(this._index + size);

		if (this._array.Length - newIndex >= 0) return false;

		length = this.RoundUpPow2Ceiling(newIndex);

		return true;
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (this._disposed) return;

		if (this._array != null)
		{
			this._pool.Return(this._array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());

			this._array = null;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public int RoundUpPow2Ceiling(int x)
	{
		checked
		{
			--x;
			x |= x >> 1;
			x |= x >> 2;
			x |= x >> 4;
			x |= x >> 8;
			x |= x >> 16;
			++x;
		}
		return x;
	}

	[StackTraceHidden]
	[DoesNotReturn]
	private void ThrowIfDisposed()
	{
		if (this._disposed) throw new ObjectDisposedException("The buffer has been disposed.");
	}
}