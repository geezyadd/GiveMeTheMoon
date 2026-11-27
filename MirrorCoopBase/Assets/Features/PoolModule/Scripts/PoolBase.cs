using System.Collections.Generic;

namespace Features.PoolModule {
    // The life of reusable items shared by every pool (ported from FLEXUS): prewarmed items wait free, a taken one is
    // active until it is released and waits free again. Past MaxSize items the active one at GetRecycleIndex is taken
    // back and reused; a pool whose items must never be taken from their user (a server's projectile, a networked
    // object) gives int.MaxValue.
    public abstract class PoolBase<T> where T : class {
        private readonly Queue<T> _free = new();
        private readonly List<T> _active = new();
        private readonly List<T> _all = new();

        protected IReadOnlyList<T> Active => _active;
        protected IReadOnlyList<T> All => _all;
        protected abstract int MaxSize { get; }

        protected void Prewarm(int count) {
            for (int i = 0; i < count && _all.Count < MaxSize; i++)
                _free.Enqueue(CreateItem());
        }

        protected T Get() {
            T item = TakeFree();
            _active.Add(item);
            OnGet(item);
            return item;
        }

        protected void Release(T item) {
            if (_active.Remove(item) == false)
                return;

            OnRelease(item);
            _free.Enqueue(item);
        }

        // An active item destroyed from outside (with the object it followed) leaves the pool without going back.
        protected void Forget(T item) {
            if (_active.Remove(item))
                _all.Remove(item);
        }

        protected void Clear() {
            _free.Clear();
            _active.Clear();
            _all.Clear();
        }

        protected abstract T Create();

        protected virtual void OnGet(T item) {
        }

        protected virtual void OnRelease(T item) {
        }

        // A free item destroyed from outside is dropped instead of taken.
        protected virtual bool IsAlive(T item) => true;

        protected virtual int GetRecycleIndex() => 0;

        private T TakeFree() {
            while (_free.Count > 0) {
                T item = _free.Dequeue();
                if (IsAlive(item))
                    return item;

                _all.Remove(item);
            }

            if (_all.Count < MaxSize)
                return CreateItem();

            Release(_active[GetRecycleIndex()]);
            return _free.Dequeue();
        }

        private T CreateItem() {
            T item = Create();
            _all.Add(item);
            return item;
        }
    }
}
