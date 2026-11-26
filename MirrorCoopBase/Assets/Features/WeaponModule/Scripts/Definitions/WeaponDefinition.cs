using System;
using System.Collections.Generic;
using Features.WeaponModule.Scripts.Parts.Delivery;
using Features.WeaponModule.Scripts.Parts.Effects;
using Features.WeaponModule.Scripts.Parts.FireModes;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Definitions {
    // One weapon as data: its numbers and its swappable parts. A new weapon is a new asset (and, if needed, one new
    // part type), never a branch on a weapon type in code.
    [CreateAssetMenu(menuName = "Game/Weapon Definition", fileName = "WeaponDefinition")]
    public sealed class WeaponDefinition : ScriptableObject {
        [Tooltip("Stable id used on the network and in saves; never rename it once used.")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [Tooltip("Shots per second at full readiness; the server's fire rate budget is built from it.")]
        [SerializeField, Min(0f)] private float _shotsPerSecond;
        [SerializeField, Min(0f)] private float _damagePerHit;
        [SerializeField, Min(0f)] private float _rangeMeters;
        [SerializeReference, WeaponPartPicker] private IWeaponFireModeDefinition _fireMode;
        [SerializeReference, WeaponPartPicker] private IShotDeliveryDefinition _delivery;
        [SerializeReference, WeaponPartPicker] private IHitEffect[] _hitEffects = Array.Empty<IHitEffect>();
        [SerializeField] private WeaponFeelProfile _feelProfile;
        [Header("Magazine")]
        [Tooltip("Rounds in a full magazine; 0: the weapon never runs out and never reloads (a turret).")]
        [SerializeField, Min(0)] private int _magazineCapacity;
        [SerializeField, Min(0f)] private float _reloadSeconds;
        [Header("Bodies struck")]
        [Tooltip("Velocity change a struck body part gets along the shot, m/s (a player's ragdoll limb).")]
        [SerializeField, Min(0f)] private float _impactPushSpeed;
        [Tooltip("How long a struck body goes limp, s; 0 only pushes it.")]
        [SerializeField, Min(0f)] private float _impactKnockdownSeconds;
        [Header("Projectile")]
        [Tooltip("The projectile each shot launches; empty for a weapon whose shots land at once (hitscan).")]
        [SerializeField] private ProjectileDefinition _projectile;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float ShotsPerSecond => _shotsPerSecond;
        public float DamagePerHit => _damagePerHit;
        public float RangeMeters => _rangeMeters;
        public IWeaponFireModeDefinition FireMode => _fireMode;
        public IShotDeliveryDefinition Delivery => _delivery;
        public IReadOnlyList<IHitEffect> HitEffects => _hitEffects;
        public WeaponFeelProfile FeelProfile => _feelProfile;
        public int MagazineCapacity => _magazineCapacity;
        public float ReloadSeconds => _reloadSeconds;
        public float ImpactPushSpeed => _impactPushSpeed;
        public float ImpactKnockdownSeconds => _impactKnockdownSeconds;
        public ProjectileDefinition Projectile => _projectile;
    }
}
