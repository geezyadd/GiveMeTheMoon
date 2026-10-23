namespace Features.WeaponModule.Scripts.Parts.FireModes {
    // The serialized settings of a fire mode inside a weapon definition. Every holder gets its own runtime fire mode,
    // so the shared asset is never changed at runtime.
    public interface IWeaponFireModeDefinition {
        public IWeaponFireMode CreateFireMode(float shotsPerSecond);
    }
}
