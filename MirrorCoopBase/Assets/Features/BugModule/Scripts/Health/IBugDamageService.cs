namespace Features.BugModule.Scripts.Health {
    public interface IBugDamageService {
        public bool IsAlive(int bugId);
        // Server: a weapon hit takes its damage; at no health left the bug is killed.
        public void ServerApplyHit(int bugId, float damage);
    }
}
