namespace Features.BugModule.Scripts.Health {
    // A bug's field agent id as a weapon target id and back.
    public interface IBugWeaponIds {
        public ulong GetTargetId(int bugId);
        public bool TryGetBugId(ulong targetId, out int bugId);
    }
}
