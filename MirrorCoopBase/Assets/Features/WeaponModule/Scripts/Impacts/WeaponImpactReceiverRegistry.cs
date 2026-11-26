using System.Collections.Generic;
using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Impacts {
    public sealed class WeaponImpactReceiverRegistry : IWeaponImpactReceiverRegistry, IWeaponImpactLookup {
        private readonly List<IWeaponImpactReceiver> _receivers = new();
        private readonly Dictionary<Rigidbody, (IWeaponImpactReceiver Receiver, int PartIndex)> _byPart = new();

        public void Add(IWeaponImpactReceiver receiver) {
            if (receiver == null || _receivers.Contains(receiver))
                return;

            _receivers.Add(receiver);
            IReadOnlyList<Rigidbody> parts = receiver.Parts;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] != null)
                    _byPart[parts[i]] = (receiver, i);
        }

        public void Remove(IWeaponImpactReceiver receiver) {
            if (_receivers.Remove(receiver) == false)
                return;

            IReadOnlyList<Rigidbody> parts = receiver.Parts;
            for (int i = 0; i < parts.Count; i++)
                if (parts[i] != null && _byPart.TryGetValue(parts[i], out var entry) && ReferenceEquals(entry.Receiver, receiver))
                    _byPart.Remove(parts[i]);
        }

        public bool TryFindByPart(Rigidbody part, out IWeaponImpactReceiver receiver, out int partIndex) {
            if (part != null && _byPart.TryGetValue(part, out var entry)) {
                receiver = entry.Receiver;
                partIndex = entry.PartIndex;
                return true;
            }

            receiver = null;
            partIndex = -1;
            return false;
        }

        public bool TryGet(uint networkId, out IWeaponImpactReceiver receiver) {
            for (int i = 0; i < _receivers.Count; i++)
                if (_receivers[i].NetworkId == networkId) {
                    receiver = _receivers[i];
                    return true;
                }

            receiver = null;
            return false;
        }
    }
}
