namespace Features.BugModule.Scripts.Configuration {
    // The bug's field agent kind number, as it goes over the network in the agent batch.
    public interface IBugKindService {
        public bool TryGetKindIndex(out byte kindIndex);
        public bool IsBug(byte kindIndex);
    }
}
