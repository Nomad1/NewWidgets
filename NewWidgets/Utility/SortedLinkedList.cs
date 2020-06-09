using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NewWidgets.Utility
{
    /// <summary>
    /// Sorted list collection that allows duplicates. It is based on LinkedList in this implementation
    /// </summary>
    [Serializable]
    public class SortedLinkedList<TKey, TValue> : LinkedList<KeyValuePair<TKey, TValue>>
        where TKey : IComparable<TKey>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RunMobile.Utility.SortedLinkedList`2"/> class.
        /// </summary>
        public SortedLinkedList()
        {
        }

        protected SortedLinkedList(SerializationInfo info, StreamingContext context)
            :base(info, context)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RunMobile.Utility.SortedLinkedList`2"/> class.
        /// </summary>
        /// <param name="collection">Collection.</param>
        public SortedLinkedList(IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            foreach (KeyValuePair<TKey, TValue> pair in collection)
                Add(pair);
        }

        /// <summary>
        /// Add the specified key and value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public LinkedListNode<KeyValuePair<TKey, TValue>> Add(TKey key, TValue value)
        {
            return Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        /// <Docs>The item to add to the current collection.</Docs>
        /// <para>Adds an item to the current collection.</para>
        /// <remarks>To be added.</remarks>
        /// <exception cref="System.NotSupportedException">The current collection is read-only.</exception>
        /// <summary>
        /// Add the specified pair.
        /// </summary>
        /// <param name="pair">Pair.</param>
        public LinkedListNode<KeyValuePair<TKey, TValue>> Add(KeyValuePair<TKey, TValue> pair)
        {
            if (Count == 0)
            {
                return AddFirst(pair);
            }
            
            if (pair.Key.CompareTo(Last.Value.Key) >= 0)
            {
                return AddLast(pair);
            }

            LinkedListNode<KeyValuePair<TKey, TValue>> node = First;
            do
            {
                if (pair.Key.CompareTo(node.Value.Key) < 0)
                {
                    return AddBefore(node, pair);
                }
                node = node.Next;
            }
            while (node != null);
            return null;
        }

        /// <summary>
        /// Tries to add the value before specific node
        /// </summary>
        /// <returns>The less.</returns>
        /// <param name="last">Last.</param>
        /// <param name="pair">Pair.</param>
        public LinkedListNode<KeyValuePair<TKey, TValue>> AddLess(LinkedListNode<KeyValuePair<TKey, TValue>> last, KeyValuePair<TKey, TValue> pair)
        {
            LinkedListNode<KeyValuePair<TKey, TValue>> node = last;

            do
            {
                if (pair.Key.CompareTo(node.Value.Key) < 0)
                {
                    return AddBefore(node, pair);
                }
                node = node.Previous;
            }
            while (node != null);

            return AddFirst(pair);
        }

        /// <summary>
        /// Retrieves first value (with smallest TKey) and removes it from the list
        /// </summary>
        public KeyValuePair<TKey, TValue> Dequeue()
        {
            KeyValuePair<TKey, TValue> result = First.Value;
            RemoveFirst();
            return result;
        }

        /// <summary>
        /// Copies all values to array. Values remain sorted
        /// </summary>
        /// <returns>The array.</returns>
        public TValue [] ToArray()
        {
            TValue [] result = new TValue[Count];

            if (result.Length > 0)
            {
                LinkedListNode<KeyValuePair<TKey, TValue>> node = First;

                int count = 0;
                do
                {
                    result[count] = node.Value.Value;
                    node = node.Next;
                    count++;
                }
                while (node != null);
            }
            return result;
        }

		public bool Remove(TValue obj)
		{
            LinkedListNode<KeyValuePair<TKey, TValue>> node = First;
            do
            {
				if (node.Value.Value.Equals(obj))
                {
					Remove(node);
                    return true;
                }
                node = node.Next;
            }
            while (node != null);
			return false;
		}
    }
}
