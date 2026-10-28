using Features.BugModule.Scripts.Brain;

namespace Features.BugModule.Scripts.Health {
    // The bug's health in the bug model goes down by each hit; the hit that leaves none kills it.
    public sealed class BugDamageService : IBugDamageService {
        private readonly BugServerModel _bugServerModel;
        private readonly IBugRemovalService _bugRemovalService;

        public BugDamageService(BugServerModel bugServerModel, IBugRemovalService bugRemovalService) {
            _bugServerModel = bugServerModel;
            _bugRemovalService = bugRemovalService;
        }

        public bool IsAlive(int bugId) =>
            _bugServerModel.TryGet(bugId, out BugAgent bug) && bug.IsDead == false;

        public void ServerApplyHit(int bugId, float damage) {
            if (_bugServerModel.TryGet(bugId, out BugAgent bug) == false || bug.IsDead)
                return;

            bug.TakeDamage(damage);
            if (bug.IsDead)
                _bugRemovalService.ServerKill(bugId);
        }
    }
}
