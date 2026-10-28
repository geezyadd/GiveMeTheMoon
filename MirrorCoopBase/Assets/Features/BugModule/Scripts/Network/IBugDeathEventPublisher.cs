namespace Features.BugModule.Scripts.Network {
    public interface IBugDeathEventPublisher {
        public void PublishDeath(int bugId);
    }
}
