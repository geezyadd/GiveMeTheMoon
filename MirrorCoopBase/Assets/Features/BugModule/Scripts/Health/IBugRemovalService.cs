namespace Features.BugModule.Scripts.Health {
    // Server: the ways a bug ends. Either way a cargo it carries is let go where it is.
    public interface IBugRemovalService {
        // Shot down: every peer plays its death where it shows the bug.
        public void ServerKill(int bugId);
        // Flew off out of the field: it is simply gone.
        public void ServerRemoveGone(int bugId);
    }
}
