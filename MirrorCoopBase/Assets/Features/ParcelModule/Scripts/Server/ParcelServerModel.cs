using System.Collections.Generic;
using Features.ParcelModule.Scripts.Stand;

namespace Features.ParcelModule.Scripts.Server {
    // Server: every spawned parcel crate by its net id, in the order they spawned, and the station's delivery stand.
    // Written by the parcel services.
    public sealed class ParcelServerModel {
        private readonly Dictionary<uint, ParcelServerRecord> _records = new();
        private readonly List<ParcelServerRecord> _ordered = new();

        public IReadOnlyList<ParcelServerRecord> Records => _ordered;
        public ParcelStand Stand { get; private set; }

        public bool TryGet(uint netId, out ParcelServerRecord record) =>
            _records.TryGetValue(netId, out record);

        public void Add(ParcelServerRecord record) {
            _records.Add(record.NetId, record);
            _ordered.Add(record);
        }

        public void SetStand(ParcelStand stand) =>
            Stand = stand;

        public void Remove(uint netId) {
            if (_records.Remove(netId, out ParcelServerRecord record))
                _ordered.Remove(record);
        }
    }
}
