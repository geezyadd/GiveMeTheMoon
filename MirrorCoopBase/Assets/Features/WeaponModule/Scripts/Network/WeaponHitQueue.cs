namespace Features.WeaponModule.Scripts.Network {
    // The shooter's hits waiting for the next network tick, oldest first. When the queue is full (a long network
    // hitch) newer hits are dropped: the server's budget would refuse such a burst anyway.
    public sealed class WeaponHitQueue {
        private const int CAPACITY = 32;

        private readonly WeaponHitReport[] _hits = new WeaponHitReport[CAPACITY];

        private int _first;

        public int Count { get; private set; }

        public bool TryEnqueue(in WeaponHitReport hit) {
            if (Count == CAPACITY)
                return false;

            _hits[(_first + Count) % CAPACITY] = hit;
            Count++;
            return true;
        }

        public WeaponHitBatch DequeueBatch(int weaponIndex) {
            WeaponHitBatch batch = new WeaponHitBatch { WeaponIndex = weaponIndex };
            while (Count > 0 && batch.IsFull == false) {
                batch.Add(_hits[_first]);
                _first = (_first + 1) % CAPACITY;
                Count--;
            }

            return batch;
        }

        public void Clear() {
            _first = 0;
            Count = 0;
        }
    }
}
