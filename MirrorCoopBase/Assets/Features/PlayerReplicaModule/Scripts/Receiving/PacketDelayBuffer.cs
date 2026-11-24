using System.Collections.Generic;
using Features.PlayerReplicaModule.Packets;

namespace Features.PlayerReplicaModule.Receiving {
    // Holds a copy's packets until the moment they show: a packet becomes current once the copy's show time (the
    // server time minus the copy delay) reaches its stamp. Packets come in order (the sequence filter is before), so
    // the buffer only takes from its front. A packet that arrives already past its moment becomes current at once.
    public sealed class PacketDelayBuffer {
        // Far more than ever waits (30 packets a second for a fraction of a second); a stuck clock cannot grow it.
        private const int CAPACITY = 64;

        private readonly Queue<BodyStatePacket> _waiting = new();

        public bool HasCurrent { get; private set; }
        public BodyStatePacket Current { get; private set; }

        public void Add(in BodyStatePacket packet) {
            if (_waiting.Count >= CAPACITY)
                _waiting.Dequeue();

            _waiting.Enqueue(packet);
        }

        // Makes the newest packet whose moment has come current; true when the current packet changed.
        public bool Advance(double showTime) {
            bool changed = false;
            while (_waiting.Count > 0 && _waiting.Peek().ServerTime <= showTime) {
                Current = _waiting.Dequeue();
                HasCurrent = true;
                changed = true;
            }

            return changed;
        }

        // Starts over from one packet shown at once, dropping what waits.
        public void Restart(in BodyStatePacket current) {
            _waiting.Clear();
            Current = current;
            HasCurrent = true;
        }

        public void Clear() {
            _waiting.Clear();
            HasCurrent = false;
        }
    }
}
