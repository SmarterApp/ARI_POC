using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ARI_POC
{
    public class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private class ValueCollection : ICollection<TValue>
        {
            private class VCEnumerator : IEnumerator<TValue>
            {
                IEnumerator<KeyValuePair<TKey, TValue> > fEnumerator;

                public VCEnumerator(IEnumerator<KeyValuePair<TKey, TValue> > enumerator)
                {
                    fEnumerator = enumerator;
                }

                public TValue Current
                {
                    get { return fEnumerator.Current.Value; }
                }

                public void Dispose()
                {
                    fEnumerator.Dispose();
                }

                object System.Collections.IEnumerator.Current
                {
                    get { return fEnumerator.Current.Value; }
                }

                public bool MoveNext()
                {
                    return fEnumerator.MoveNext();
                }

                public void Reset()
                {
                    fEnumerator.Reset();
                }
            }

            OrderedDictionary<TKey, TValue> fDictionary;

            public ValueCollection(OrderedDictionary<TKey, TValue> dictionary)
            {
                fDictionary = dictionary;
            }

            public void Add(TValue item)
            {
                throw new InvalidOperationException();
            }

            public void Clear()
            {
                throw new InvalidOperationException();
            }

            public bool Contains(TValue item)
            {
                foreach(var pair in fDictionary.fList)
                {
                    if (Object.Equals(pair.Value, item)) return true;
                }
                return false;
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                foreach(var pair in fDictionary.fList)
                {
                    array[arrayIndex++] = pair.Value;
                }
            }

            public int Count
            {
                get { return fDictionary.Count; }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public bool Remove(TValue item)
            {
                throw new InvalidOperationException();
            }

            public IEnumerator<TValue> GetEnumerator()
            {
                return new VCEnumerator(fDictionary.fList.GetEnumerator());
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return new VCEnumerator(fDictionary.fList.GetEnumerator());
            }
        }

        List<KeyValuePair<TKey, TValue>> fList = new List<KeyValuePair<TKey, TValue>>();
        Dictionary<TKey, int> fIndex = new Dictionary<TKey, int>();

        public void Add(TKey key, TValue value)
        {
            if (fIndex.ContainsKey(key)) throw new ArgumentException("OrderedDictionary.Add: Key already exists.");
            fList.Add(new KeyValuePair<TKey, TValue>(key, value));
            fIndex[key] = fList.Count - 1;
        }

        public bool ContainsKey(TKey key)
        {
            return fIndex.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return fIndex.Keys; }
        }

        public bool Remove(TKey key)
        {
            int i;
            if (!fIndex.TryGetValue(key, out i)) return false;
            fList.RemoveAt(i);
            fIndex.Remove(key);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            int i;
            if (!fIndex.TryGetValue(key, out i))
            {
                value = default(TValue);
                return false;
            }
            value = fList[i].Value;
            return true;
        }

        private ValueCollection fValueCollection = null;

        public ICollection<TValue> Values
        {
            get
            {
                if (fValueCollection == null)
                {
                    fValueCollection = new ValueCollection(this);
                }

                return fValueCollection;
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                return fList[fIndex[key]].Value;
            }
            set
            {
                int i;
                if (fIndex.TryGetValue(key, out i))
                {
                    fList[i] = new KeyValuePair<TKey, TValue>(key, value);
                }
                else
                {
                    fList.Add(new KeyValuePair<TKey, TValue>(key, value));
                    fIndex[key] = fList.Count - 1;
                }
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            fList.Add(item);
            fIndex[item.Key] = fList.Count - 1;
        }

        public void Clear()
        {
            fList.Clear();
            fIndex.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            int i;
            if (fIndex.TryGetValue(item.Key, out i)) return false;
            return Object.Equals(fList[i].Value, item.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach(var pair in fList)
            {
                array[arrayIndex++] = pair;
            }
        }

        public int Count
        {
            get { return fList.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            int i;
            if (fIndex.TryGetValue(item.Key, out i)) return false;
            if (!Object.Equals(fList[i].Value, item.Value)) return false;
            fList.RemoveAt(i);
            fIndex.Remove(item.Key);
            return true;
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return fList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return fList.GetEnumerator();
        }
    }
}