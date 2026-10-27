using System;

namespace Features.WeaponModule.Scripts.Parts.FireModes {
    // A press fires one shot; the weapon's shots per second set the cooldown before the next press may fire.
    [Serializable]
    public sealed class SingleShotFireModeDefinition : IWeaponFireModeDefinition {
        public IWeaponFireMode CreateFireMode(float shotsPerSecond) =>
            new SingleShotFireMode(1f / shotsPerSecond);
    }
}
