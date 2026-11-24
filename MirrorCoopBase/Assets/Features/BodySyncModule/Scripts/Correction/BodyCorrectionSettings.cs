namespace Features.BodySyncModule.Correction {
    // The numbers of the copy correction. The game fills them from the body sync configuration; tests set their own.
    public struct BodyCorrectionSettings {
        // Share of the horizontal error removed per physics step (0.1 at 50 steps a second halves it in about 0.13 s).
        public float PositionPull;
        // While the owner presses no walk key: no horizontal pull below this error, metres.
        public float IdleDeadZone;
        // While the owner presses no walk key: share of the horizontal error removed per physics step.
        public float IdlePositionPull;
        // Below this vertical error, metres, the network does not touch the height: jumps, steps and falls stay local.
        public float VerticalDeadZoneMin;
        // From this vertical error, metres, the vertical pull works at full strength.
        public float VerticalDeadZoneMax;
        // The largest vertical move per physics step, metres.
        public float VerticalPullPerStep;
        // How far ahead the target is moved along the packet's velocity at most, seconds.
        public float MaxExtrapolationSeconds;
        // Past this error, metres, the copy is moved to the target at once.
        public float SnapDistance;
        // How long the owner and the copy may disagree on standing or flying, seconds, before the dead zone is dropped.
        public float GroundMismatchSeconds;
    }
}
