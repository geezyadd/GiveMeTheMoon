namespace Features.PlayerReplicaModule.Correction {
    // Counts how long the owner and the copy disagree on standing or flying. A short disagreement is normal (the copy
    // lands a moment later); a long one means the copy is stuck on an edge or dropped onto a lower step, and then the
    // vertical correction works without its dead zone.
    public sealed class GroundMismatchTimer {
        private float _mismatchSeconds;

        public void Tick(bool ownerGrounded, bool copyGrounded, float deltaSeconds) =>
            _mismatchSeconds = ownerGrounded == copyGrounded ? 0f : _mismatchSeconds + deltaSeconds;

        public bool IsStuck(float limitSeconds) =>
            _mismatchSeconds > limitSeconds;

        public void Reset() =>
            _mismatchSeconds = 0f;
    }
}
