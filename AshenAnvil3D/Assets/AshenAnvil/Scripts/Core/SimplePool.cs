using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshenAnvil
{
    /// <summary>Allocation-free reuse of frequently spawned objects (projectiles, VFX, texts).</summary>
    public class SimplePool<T> where T : Component
    {
        readonly Stack<T> free = new Stack<T>();
        readonly Func<T> factory;
        readonly Transform root;

        public SimplePool(Func<T> factory, Transform root = null)
        {
            this.factory = factory;
            this.root = root;
        }

        public T Get()
        {
            T item = null;
            while (free.Count > 0 && item == null) item = free.Pop();
            if (item == null) item = factory();
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (item == null) return;
            item.gameObject.SetActive(false);
            if (root != null) item.transform.SetParent(root, false);
            free.Push(item);
        }

        public void Prewarm(int count)
        {
            var temp = new List<T>();
            for (int i = 0; i < count; i++) temp.Add(Get());
            foreach (var t in temp) Release(t);
        }
    }
}
