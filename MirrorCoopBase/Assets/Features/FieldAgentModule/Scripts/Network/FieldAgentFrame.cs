using System;

namespace Features.FieldAgentModule.Scripts.Network {
    // A batch of every agent at one server time, sorted by id: the whole synced state of the agents, so a player who
    // joins gets the current agents with the next batch. Two batches are the same when their sequence is.
    [Serializable]
    public struct FieldAgentFrame : IEquatable<FieldAgentFrame> {
        public int Sequence;
        public double ServerTime;
        public FieldAgentSnapshot[] Agents;

        public FieldAgentFrame(int sequence, double serverTime, FieldAgentSnapshot[] agents) {
            Sequence = sequence;
            ServerTime = serverTime;
            Agents = agents;
        }

        public bool Equals(FieldAgentFrame other) =>
            Sequence == other.Sequence;

        public override bool Equals(object other) =>
            other is FieldAgentFrame frame && Equals(frame);

        public override int GetHashCode() =>
            Sequence;
    }
}
