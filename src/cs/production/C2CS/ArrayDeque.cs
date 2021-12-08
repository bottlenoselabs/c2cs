// Copyright (c) Bottlenose Labs Inc. (https://github.com/bottlenoselabs). All rights reserved.
// Licensed under the MIT license. See LICENSE file in the Git repository root directory for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace C2CS;
// TODO: Add unit tests.
// TODO: Allow for a custom resize function.
// TODO: Use a struct enumerator.

/// <summary>
///     Represents an array container of elements which can added to and removed from either the front or back in
///     amortized constant time; a double ended queue (deque).
/// </summary>
/// <typeparam name="T">The type of an element stored in the deque.</typeparam>
[PublicAPI]
public sealed class ArrayDeque<T> : IList<T>
{
    private const int DefaultCapacity = 16;
    private int _frontArrayIndex;
    private T[] _elements;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ArrayDeque{T}" /> class to be empty with default initial
    ///     capacity.
    /// </summary>
    public ArrayDeque()
    {
        _elements = Array.Empty<T>();
    }

    /// <summary>
    ///     Gets or sets the total number of elements that can be contained before resizing the internal array is
    ///     required.
    /// </summary>
    /// <returns>
    ///     A 32-bit signed integer that is non-negative.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         When changing the <see cref="Capacity" /> to be less than the <see cref="Count" />, elements will
    ///         be removed so that <see cref="Count" /> is equal to the new <see cref="Capacity" />.
    ///     </para>
    /// </remarks>
    public int Capacity
    {
        get => _elements.Length;
        set => SetCapacity(value);
    }

    private void SetCapacity(int value)
    {
        if (value == Capacity)
        {
            return;
        }

        if (value < Count)
        {
            Count = value;
        }

        if (value == 0)
        {
            _elements = Array.Empty<T>();
            return;
        }

        var elements = new T[value];
        CopyTo(elements, 0);

        _frontArrayIndex = 0;
        _elements = elements;
    }

    /// <summary>
    ///     Gets or sets the element at the specified index in the <see cref="ArrayDeque{T}" />.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get or set.</param>
    /// <returns>The element at the specified index.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="index" /> was out of range; it must be non-negative and less than <see cref="Count" />.
    /// </exception>
    public T this[int index]
    {
        get
        {
            var arrayIndex = GetArrayIndex(index);
            if (arrayIndex == -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    "Index was out of range; it must be non-negative and less than count.");
            }

            return _elements[arrayIndex];
        }

        set
        {
            var arrayIndex = GetArrayIndex(index);
            if (arrayIndex == -1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    "Index was out of range; it must be non-negative and less than count.");
            }

            _elements[arrayIndex] = value;
        }
    }

    /// <summary>
    ///     Gets the number of elements contained in the <see cref="ArrayDeque{T}" />.
    /// </summary>
    /// <returns>A 32-bit signed integer that is non-negative.</returns>
    public int Count { get; private set; }

    /// <summary>
    ///     Adds an element to the front of the <see cref="ArrayDeque{T}" />.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <remarks>
    ///     <para>
    ///         This method is amortized constant time, O(1)+, operation.
    ///     </para>
    /// </remarks>
    public void PushFront(T item)
    {
        EnsureCapacity(Count + 1);
        _frontArrayIndex = (_frontArrayIndex - 1 + _elements.Length) % _elements.Length;
        _elements[_frontArrayIndex] = item;
        Count++;
    }

    /// <summary>
    ///     Adds an element to the back of the <see cref="ArrayDeque{T}" />.
    /// </summary>
    /// <param name="item">The element to add.</param>
    /// <remarks>
    ///     <para>
    ///         This method is amortized constant time, O(1)+, operation.
    ///     </para>
    /// </remarks>
    public void PushBack(T item)
    {
        EnsureCapacity(Count + 1);
        var index = (_frontArrayIndex + Count++) % _elements.Length;
        _elements[index] = item;
    }

    /// <summary>
    ///     Gets and removes the element at the front of the <see cref="ArrayDeque{T}" />.
    /// </summary>
    /// <returns>
    ///     The element at the front of the <see cref="ArrayDeque{T}" /> if the <see cref="ArrayDeque{T}" /> is not
    ///     empty; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is an O(1) operation.
    ///     </para>
    /// </remarks>
    public T? PopFront()
    {
        if (Count == 0)
        {
            return default;
        }

        var index = _frontArrayIndex % _elements.Length;
        var element = _elements[index];
        _elements[index] = default!;
        _frontArrayIndex = (_frontArrayIndex + 1) % _elements.Length;
        Count--;
        return element;
    }

    /// <summary>
    ///     Gets and removes the element at the back of the <see cref="ArrayDeque{T}" />.
    /// </summary>
    /// <returns>
    ///     The element at the end of the <see cref="ArrayDeque{T}" /> if the <see cref="ArrayDeque{T}" /> is not
    ///     empty; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    ///     <para>
    ///         This method is an O(1) operation.
    ///     </para>
    /// </remarks>
    public T? PopBack()
    {
        if (Count == 0)
        {
            return default;
        }

        var circularBackIndex = (_frontArrayIndex + (Count - 1)) % _elements.Length;
        var element = _elements[circularBackIndex];
        _elements[circularBackIndex] = default!;
        Count--;
        return element;
    }

    /// <summary>
    ///     Gets the element at the specified index in the <see cref="ArrayDeque{T}" />.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>
    ///     The element at the <paramref name="index" /> of the <see cref="ArrayDeque{T}" /> if the
    ///     <paramref name="index" /> is in range; otherwise, <c>default</c>.
    /// </returns>
    public T? Get(int index)
    {
        var arrayIndex = GetArrayIndex(index);
        return arrayIndex == -1 ? default : _elements[arrayIndex];
    }

    /// <summary>
    ///     Gets the element at the front of the <see cref="ArrayDeque{T}" />.
    /// </summary>
    /// <returns>
    ///     The element at the front of the <see cref="ArrayDeque{T}" /> if the <see cref="ArrayDeque{T}" /> is not
    ///     empty; otherwise, <c>default</c>.
    /// </returns>
    public T? PeekFront()
    {
        return Get(0);
    }

    /// <summary>
    ///     Gets the element at the back of the <see cref="ArrayDeque{T}" />.
    /// </summary>
    /// <returns>
    ///     The element at the back of the <see cref="ArrayDeque{T}" /> if the <see cref="ArrayDeque{T}" /> is not
    ///     empty; otherwise, <c>default</c>.
    /// </returns>
    public T? PeekBack()
    {
        return Get(Count - 1);
    }

    bool ICollection<T>.IsReadOnly => false;

    /// <summary>
    ///     Returns an enumerator that iterates through the elements.
    /// </summary>
    /// <returns>An <see cref="IEnumerator{T}" />.</returns>
    public IEnumerator<T> GetEnumerator()
    {
        if (Count == 0)
        {
            yield break;
        }

        if (Count <= _elements.Length - _frontArrayIndex)
        {
            for (var i = _frontArrayIndex; i < _frontArrayIndex + Count; i++)
            {
                yield return _elements[i];
            }
        }
        else
        {
            for (var i = _frontArrayIndex; i < Capacity; i++)
            {
                yield return _elements[i];
            }

            for (var i = 0; i < (_frontArrayIndex + Count) % Capacity; i++)
            {
                yield return _elements[i];
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    void ICollection<T>.Add(T item)
    {
        PushBack(item);
    }

    public int IndexOf(T item)
    {
        throw new NotImplementedException();
    }

    void IList<T>.Insert(int index, T item)
    {
        throw new NotImplementedException();
    }

    bool ICollection<T>.Remove(T item)
    {
        throw new NotImplementedException();
    }

    void IList<T>.RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Removes all elements without changing the <see cref="Capacity" />.
    /// </summary>
    public void Clear()
    {
        if (Count == 0)
        {
            return;
        }

        if (Count > _elements.Length - _frontArrayIndex)
        {
            Array.Clear(_elements, _frontArrayIndex, _elements.Length - _frontArrayIndex);
            Array.Clear(_elements, 0, _frontArrayIndex + Count - _elements.Length);
        }
        else
        {
            Array.Clear(_elements, _frontArrayIndex, Count);
        }

        Count = 0;
        _frontArrayIndex = 0;
    }

    public bool Contains(T item)
    {
        throw new NotImplementedException();
    }

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
        CopyTo(array, arrayIndex);
    }

    private void CopyTo(T[] array, int arrayIndex)
    {
        if (arrayIndex < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(arrayIndex),
                "Index was less than the array's lower bound.");
        }

        if (arrayIndex >= array.Length)
        {
            throw new ArgumentOutOfRangeException(
                nameof(arrayIndex),
                "Index was greater than the array's upper bound.");
        }

        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("Destination array was not long enough.");
        }

        if (Count == 0)
        {
            return;
        }

        var loopsAround = Count > _elements.Length - _frontArrayIndex;
        if (!loopsAround)
        {
            Array.Copy(_elements, _frontArrayIndex, array, arrayIndex, Count);
        }
        else
        {
            Array.Copy(_elements, _frontArrayIndex, array, arrayIndex, Capacity - _frontArrayIndex);
            Array.Copy(
                _elements,
                0,
                array,
                arrayIndex + Capacity - _frontArrayIndex,
                _frontArrayIndex + (Count - Capacity));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetArrayIndex(int index)
    {
        if (index < 0 || index >= Count)
        {
            return -1;
        }

        return _elements.Length != 0 ? (_frontArrayIndex + index) % _elements.Length : 0;
    }

    private void EnsureCapacity(int minimum)
    {
        if (_elements.Length >= minimum)
        {
            return;
        }

        var newCapacity = DefaultCapacity;
        if (_elements.Length > 0)
        {
            newCapacity = _elements.Length * 2;
        }

        newCapacity = Math.Max(newCapacity, minimum);
        Capacity = newCapacity;
    }
}
