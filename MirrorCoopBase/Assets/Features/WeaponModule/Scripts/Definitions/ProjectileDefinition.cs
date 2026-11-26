using Features.ExplosionModule.Configuration;
using Features.WeaponModule.Scripts.Projectiles;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Definitions {
    // One kind of projectile as data: its model, how it flies and how it ends. A grenade falls along a ballistic arc
    // and bounces until its fuse runs out; a rocket flies straight and goes off where it strikes. Either goes off
    // when its lifetime is over.
    [CreateAssetMenu(menuName = "Game/Projectile Definition", fileName = "ProjectileDefinition")]
    public sealed class ProjectileDefinition : ScriptableObject {
        [SerializeField] private Projectile _prefab;
        [Tooltip("Launch speed from the muzzle, m/s.")]
        [SerializeField, Min(0f)] private float _speed = 20f;
        [Tooltip("On: it falls under gravity and bounces (a grenade). Off: it keeps its speed along a straight line (a rocket).")]
        [SerializeField] private bool _usesGravity = true;
        [Tooltip("It goes off after this long in flight, s, if nothing set it off before.")]
        [SerializeField, Min(0.05f)] private float _lifetimeSeconds = 2.5f;
        [Tooltip("It goes off at the first thing it strikes.")]
        [SerializeField] private bool _explodesOnImpact;
        [Tooltip("It goes off when it strikes a moving body (a player's ragdoll, a loose item), even if it bounces off the rest.")]
        [SerializeField] private bool _explodesOnBodyImpact = true;
        [SerializeField] private ExplosionPreset _explosion;

        public Projectile Prefab => _prefab;
        public float Speed => _speed;
        public bool UsesGravity => _usesGravity;
        public float LifetimeSeconds => _lifetimeSeconds;
        public bool ExplodesOnImpact => _explodesOnImpact;
        public bool ExplodesOnBodyImpact => _explodesOnBodyImpact;
        public ExplosionPreset Explosion => _explosion;
    }
}
