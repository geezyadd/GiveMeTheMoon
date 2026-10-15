using System;

namespace Game.Connection
{
    // Client: why the host turned this player away the last time; cleared when the player tries again.
    public sealed class ConnectionRejectModel
    {
        public ConnectionRejectReason LastRejection { get; private set; } = ConnectionRejectReason.Accepted;

        public bool HasRejection =>
            LastRejection != ConnectionRejectReason.Accepted;

        public event Action OnLastRejectionChanged;

        internal void Set(ConnectionRejectReason reason)
        {
            if (LastRejection == reason)
                return;

            LastRejection = reason;
            OnLastRejectionChanged?.Invoke();
        }

        internal void Clear() =>
            Set(ConnectionRejectReason.Accepted);
    }
}
