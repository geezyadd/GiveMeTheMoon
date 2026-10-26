#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts.Debug {
    // The weapon test dummy the host has spawned, if any (server side).
    public sealed class WeaponDebugTargetModel {
        public WeaponDebugTarget Target { get; private set; }

        public void SetTarget(WeaponDebugTarget target) =>
            Target = target;

        public void ClearTarget() =>
            Target = null;
    }
}
#endif
