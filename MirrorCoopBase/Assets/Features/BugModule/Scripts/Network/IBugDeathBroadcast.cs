namespace Features.BugModule.Scripts.Network {
    public interface IBugDeathBroadcast {
        // Server: tells every peer the bug was killed.
        public void ServerBroadcastDeath(int bugId);
    }
}
