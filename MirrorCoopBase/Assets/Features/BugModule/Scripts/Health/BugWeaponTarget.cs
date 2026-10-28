using Features.WeaponModule.Contracts;

namespace Features.BugModule.Scripts.Health {
    // Server: the bug a reported hit names, as the weapon hit path sees it. One instance serves every bug: the hit
    // path aims it at a bug and uses it within the same call, so a hit allocates nothing.
    public sealed class BugWeaponTarget : IWeaponDamageTarget {
        private readonly IBugDamageService _bugDamageService;

        private int _bugId;

        public bool IsAlive => _bugDamageService.IsAlive(_bugId);

        public BugWeaponTarget(IBugDamageService bugDamageService) =>
            _bugDamageService = bugDamageService;

        public void AimAt(int bugId) =>
            _bugId = bugId;

        public void ServerReceiveHit(in WeaponHit hit) =>
            _bugDamageService.ServerApplyHit(_bugId, hit.Damage);
    }
}
