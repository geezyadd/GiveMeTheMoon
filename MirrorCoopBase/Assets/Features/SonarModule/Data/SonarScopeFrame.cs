namespace Features.SonarModule.Data {
    // The scope's numbers this frame: range, the ping wave (below 0: no wave), the cooldown (1 = ready), whether the
    // radar works, and the crew's marks against the limit.
    public readonly struct SonarScopeFrame {
        public float RangeMeters { get; }
        public float WaveShare { get; }
        public float CooldownShare { get; }
        public bool IsWorking { get; }
        public int MarkCount { get; }
        public int MarkLimit { get; }

        public SonarScopeFrame(float rangeMeters, float waveShare, float cooldownShare, bool isWorking, int markCount, int markLimit) {
            RangeMeters = rangeMeters;
            WaveShare = waveShare;
            CooldownShare = cooldownShare;
            IsWorking = isWorking;
            MarkCount = markCount;
            MarkLimit = markLimit;
        }
    }
}
