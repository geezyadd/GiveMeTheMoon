using System;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.FireModes {
    [Serializable]
    public sealed class AutoFireModeDefinition : IWeaponFireModeDefinition {
        [Tooltip("Seconds from still barrels to full spin; the first shot comes at full spin.")]
        [SerializeField, Min(0f)] private float _spinUpSeconds;
        [Tooltip("Seconds from full spin to still barrels after the trigger is released.")]
        [SerializeField, Min(0f)] private float _spinDownSeconds;

        public float SpinUpSeconds => _spinUpSeconds;
        public float SpinDownSeconds => _spinDownSeconds;

        public IWeaponFireMode CreateFireMode(float shotsPerSecond) =>
            new AutoFireMode(_spinUpSeconds, _spinDownSeconds, shotsPerSecond);
    }
}
