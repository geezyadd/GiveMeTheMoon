namespace Features.GrabModule.Scripts.Held {
    // One frame of a held control on the holder's peer: a position to send to the server, and whether the server
    // gave the control to someone else or to no one.
    public readonly struct HeldControlFrame {
        public bool IsLost { get; }
        public bool HasPositionToSend { get; }
        public float Position { get; }

        public HeldControlFrame(bool isLost, bool hasPositionToSend, float position) {
            IsLost = isLost;
            HasPositionToSend = hasPositionToSend;
            Position = position;
        }
    }
}
