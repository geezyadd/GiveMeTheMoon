#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts.Debug {
    public interface IWeaponDebugTargets {
        public WeaponDebugTarget Target { get; }

        public WeaponDebugTarget DebugSpawnTarget();

        public void DebugDespawnTarget();
    }
}
#endif
