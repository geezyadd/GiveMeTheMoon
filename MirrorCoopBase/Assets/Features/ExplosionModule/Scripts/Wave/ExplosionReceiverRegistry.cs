using System.Collections.Generic;
using Features.ExplosionModule.Contracts;
using UnityEngine;

namespace Features.ExplosionModule.Wave {
    // The receivers on this peer and the rigidbodies they own, so the wave hands those to their receiver instead of
    // throwing them as loose bodies.
    public sealed class ExplosionReceiverRegistry : IExplosionReceiverRegistry {
        private readonly List<IExplosionReceiver> _receivers = new();
        private readonly HashSet<Rigidbody> _ownedParts = new();

        public IReadOnlyList<IExplosionReceiver> Receivers => _receivers;

        public void Add(IExplosionReceiver receiver) {
            if (receiver == null || _receivers.Contains(receiver))
                return;

            _receivers.Add(receiver);
            RebuildParts();
        }

        public void Remove(IExplosionReceiver receiver) {
            if (_receivers.Remove(receiver))
                RebuildParts();
        }

        public bool IsOwnedPart(Rigidbody body) =>
            _ownedParts.Contains(body);

        private void RebuildParts() {
            _ownedParts.Clear();
            foreach (IExplosionReceiver receiver in _receivers) {
                foreach (Rigidbody part in receiver.Parts) {
                    if (part != null)
                        _ownedParts.Add(part);
                }
            }
        }
    }
}
