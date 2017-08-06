using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Threading;

namespace ChatServer.Utility
{
    public class ControlledObservableCollection<T> : ICollection<T>, INotifyCollectionChanged
    {
        #region Properties

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private Dispatcher Dispatcher { get; }
        private ICollection<T> Collection { get; }

        public int Count => Collection.Count;
        public bool IsReadOnly => Collection.IsReadOnly;

        #endregion

        public ControlledObservableCollection(Dispatcher dispatcher)
        {
            Dispatcher = dispatcher;
            
            var collection = new ObservableCollection<T>();
            collection.CollectionChanged += (sender, args) => Synchronize(() =>
                CollectionChanged?.Invoke(this, args));

            Collection = collection;
        }

        #region Methods

        private void Synchronize(Action action) => Dispatcher.Invoke(action);

        public IEnumerator<T> GetEnumerator() => Collection.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public void Add(T item) => Synchronize(() => Collection.Add(item));

        public void Clear() => Synchronize(Collection.Clear);

        public bool Contains(T item) => Collection.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => Collection.CopyTo(array, arrayIndex);

        public bool Remove(T item)
        {
            var result = false;
            Synchronize(() => { result = Collection.Remove(item); });
            return result;
        }

        #endregion
    }
}