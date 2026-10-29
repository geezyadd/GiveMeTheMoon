namespace Features.BugModule.Scripts.Health {
    // Server: the ways a bug ends. Every way a cargo it carries is let go where it is (or was last).
    public interface IBugRemovalService {
        // Shot down: every peer plays its death where it shows the bug.
        public void ServerKill(int bugId);
        // Flew off out of the field: it is simply gone.
        public void ServerRemoveGone(int bugId);
        // Its field agent was removed from outside (all agents removed): only the bug is forgotten.
        public void ServerForgetVanished(int bugId);
        // The run's ship or the server is gone: every bug is forgotten.
        public void ServerForgetAll();
    }
}
