using System;
using System.Collections.Generic;
using Features.NetworkModelModule.Scripts;

namespace Features.ShopModule.Scripts.Data {
    // Client side: the crewmates who are dead and online right now, i.e. who can be bought back at a station.
    public sealed class DeadCrewModel {
        private readonly List<PlayerKey> _entries = new();

        public IReadOnlyList<PlayerKey> Entries =>
            _entries;

        public event Action OnEntriesChanged;

        public void SetEntries(IReadOnlyList<PlayerKey> entries) {
            if (HasSameEntries(entries))
                return;

            _entries.Clear();
            for (int i = 0; i < entries.Count; i++)
                _entries.Add(entries[i]);

            OnEntriesChanged?.Invoke();
        }

        private bool HasSameEntries(IReadOnlyList<PlayerKey> entries) {
            if (entries.Count != _entries.Count)
                return false;

            for (int i = 0; i < entries.Count; i++) {
                if (entries[i].Equals(_entries[i]) == false)
                    return false;
            }

            return true;
        }
    }
}
