using System;
using Features.FieldAgentModule.Scripts.Network;

namespace Features.FieldAgentModule.Scripts.Client {
    // A client's own copy of one received batch, reused slot by slot of the frame buffer.
    public sealed class FieldAgentBufferedFrame {
        private FieldAgentSnapshot[] _agents = Array.Empty<FieldAgentSnapshot>();

        public int Sequence { get; private set; }
        public double ServerTime { get; private set; }
        public int Count { get; private set; }
        public FieldAgentSnapshot[] Agents => _agents;

        public void CopyFrom(in FieldAgentFrame frame) {
            Sequence = frame.Sequence;
            ServerTime = frame.ServerTime;
            Count = frame.Agents?.Length ?? 0;
            if (_agents.Length < Count)
                _agents = new FieldAgentSnapshot[Count];

            if (Count > 0)
                Array.Copy(frame.Agents, _agents, Count);
        }
    }
}
