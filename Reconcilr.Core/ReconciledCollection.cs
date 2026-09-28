using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Reconcilr;

public sealed class ReconciledCollection<TSource, TShadow> : IReadOnlyList<TShadow>, IList, INotifyCollectionChanged
{
    private readonly List<TSource> _sources = new List<TSource>();
    private readonly List<TShadow> _items = new List<TShadow>();
    private readonly Func<TSource, TShadow> _createShadow;
    private readonly Func<TSource, TSource, bool> _hasSourceChanged;
    private readonly Func<TShadow, TSource, TSource, bool> _reconcileShadow;

    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public int Count => _items.Count;

    public TShadow this[int index] => _items[index];

    bool IList.IsFixedSize => true;

    bool IList.IsReadOnly => true;

    object? IList.this[int index]
    {
        get => _items[index];
        set => throw new NotSupportedException("This collection is read-only.");
    }

    int IList.Add(object? value) => throw new NotSupportedException("This collection is read-only.");

    void IList.Clear() => throw new NotSupportedException("This collection is read-only.");

    bool IList.Contains(object? value) => value is TShadow item && _items.Contains(item);

    int IList.IndexOf(object? value) => value is TShadow item ? _items.IndexOf(item) : -1;

    void IList.Insert(int index, object? value) => throw new NotSupportedException("This collection is read-only.");

    void IList.Remove(object? value) => throw new NotSupportedException("This collection is read-only.");

    void IList.RemoveAt(int index) => throw new NotSupportedException("This collection is read-only.");

    void ICollection.CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

    bool ICollection.IsSynchronized => false;

    object ICollection.SyncRoot => ((ICollection)_items).SyncRoot;

    public ReconciledCollection(
        IEnumerable<TSource> initial,
        Func<TSource, TShadow> createShadow,
        Func<TSource, TSource, bool> hasSourceChanged,
        Func<TShadow, TSource, TSource, bool> reconcileShadow)
    {
        _createShadow = createShadow;
        _hasSourceChanged = hasSourceChanged;
        _reconcileShadow = reconcileShadow;

        foreach (var source in initial)
        {
            _sources.Add(source);
            _items.Add(_createShadow(source));
        }
    }

    public IEnumerator<TShadow> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Reconcile(IEnumerable<TSource> next)
    {
        using var enumerator = next.GetEnumerator();
        var index = 0;

        while (enumerator.MoveNext())
        {
            var nextSource = enumerator.Current;
            if (index < _items.Count)
            {
                var previousSource = _sources[index];
                if (_hasSourceChanged(previousSource, nextSource))
                {
                    var shadow = _items[index];
                    if (!_reconcileShadow(shadow, previousSource, nextSource))
                    {
                        var previousShadow = shadow;
                        shadow = _createShadow(nextSource);
                        _items[index] = shadow;
                        CollectionChanged?.Invoke(this,
                            new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, shadow, previousShadow, index));
                    }

                    _sources[index] = nextSource;
                }
            }
            else
            {
                var shadow = _createShadow(nextSource);
                _sources.Add(nextSource);
                _items.Add(shadow);
                CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, shadow, index));
            }

            index++;
        }

        for (var removeIndex = _items.Count - 1; removeIndex >= index; removeIndex--)
        {
            var removedItem = _items[removeIndex];
            _sources.RemoveAt(removeIndex);
            _items.RemoveAt(removeIndex);
            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem, removeIndex));
        }
    }
}