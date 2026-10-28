using System;
using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Parts.Effects {
    // Hooks the struck target on the rope and pulls it in at a constant speed. Only a target that can be pulled
    // (IWeaponPullTarget) is hooked; on any other target (a rock) the rope finds no hold and nothing happens.
    [Serializable]
    public sealed class HookAndPullHitEffect : IHitEffect {
        [Tooltip("How fast a hooked target comes in, meters per second.")]
        [SerializeField, Min(0f)] private float _pullSpeedMetersPerSecond;
        [Tooltip("The rope breaks when its holder gets farther than this from the hooked target, meters.")]
        [SerializeField, Min(0f)] private float _maxRopeMeters;

        public float PullSpeedMetersPerSecond => _pullSpeedMetersPerSecond;
        public float MaxRopeMeters => _maxRopeMeters;

        public void Apply(in WeaponHit hit, IWeaponTarget target) {
            if (target is IWeaponPullTarget pullTarget)
                pullTarget.ServerReceivePull(hit, new WeaponPull(_pullSpeedMetersPerSecond, _maxRopeMeters));
        }
    }
}
