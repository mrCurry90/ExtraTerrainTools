using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TerrainTools.EditorHistory
{
    public class HistoryCollection<T> : IEnumerable<T>
    {
        protected List<T> _items;
        public int Now { get; protected set; }
        public int Count{ get { return _items.Count; } }
        public bool IsEmpty{ get { return Count == 0; } }
        protected static int First = 0;
        protected int Last{ get { return Count - 1; } }   

        public HistoryCollection()
        {
            _items = new();
            Now = 0;
        }

        public void Insert(T item)
        {
            if (Now < Count)
            {
                _items.RemoveRange(Now, Count - Now);
            }
            _items.Add(item);

            Now = Count;
        }

        public T RemoveAt( int index )
        {
            T temp = _items[index];
            _items.RemoveAt(index);
            return temp;
        }

        public void Clear()
        {
            _items.Clear();
            Now = 0;
        }
        
        public List<T> Undo( int steps )
        {
            int before = Now;
            Now -= steps;
            if (Now < First)
                Now = First;

            List<T> list = new(steps);
            for (int i = before - 1; i >= Now; i--)
            {
                list.Add( _items[i] );
            }            

            return list;
        }

        public List<T> Redo( int steps )
        {
            int before = Now;
            Now += steps;
            if (Now > Count)
                Now = Count;


            List<T> list = new(steps);
            for (int i = before; i < Now; i++)
            {
                list.Add( _items[i] );
            }

            return list;
        }

        public T Peek( int index )
        {
            return _items[index];
        }

        public T Peek()
        {
            return _items[Last];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}