using System;
using System.Collections.Generic;
using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Projectiles {
    // One grenade or rocket in flight on one peer, moved by Unity physics. The server's copy is the real one: it goes
    // off where it strikes or when its lifetime is over. Every other peer flies a copy of its own from the same launch,
    // which only ends (hides) on its own and goes back to the pool when the server says the real one went off.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour {
        // Speed below which a straight-flying projectile counts as stopped (struck something it could not set off on).
        private const float MIN_STRAIGHT_SPEED = 0.01f;

        [SerializeField] private Rigidbody _rigidbody;
        [SerializeField] private Collider _collider;

        private readonly List<Collider> _ignored = new();
        private ProjectileDefinition _definition;
        private Vector3 _straightVelocity;
        private float _lifetimeLeft;
        private bool _hasEnded;

        // The projectile and the point where it ended; IsAuthoritative says whether it really goes off there.
        public event Action<Projectile, Vector3> Ended;

        public ulong Id { get; private set; }
        public bool IsAuthoritative { get; private set; }
        public ProjectileDefinition Definition => _definition;
        public bool IsActive => gameObject.activeSelf;

        public void OnSpawned(ProjectileDefinition definition, Vector3 position, Vector3 velocity, ulong id, bool isAuthoritative,
                              IReadOnlyList<Collider> ignored) {
            _definition = definition;
            Id = id;
            IsAuthoritative = isAuthoritative;
            _lifetimeLeft = definition.LifetimeSeconds;
            _hasEnded = false;
            _straightVelocity = velocity;
            transform.SetPositionAndRotation(position, velocity.sqrMagnitude > 0f ? Quaternion.LookRotation(velocity) : Quaternion.identity);
            IgnoreCollisions(ignored);
            gameObject.SetActive(true);
            _rigidbody.position = position;
            _rigidbody.rotation = transform.rotation;
            _rigidbody.useGravity = definition.UsesGravity;
            _rigidbody.linearVelocity = velocity;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        public void OnDespawned() {
            RestoreCollisions();
            _definition = null;
            gameObject.SetActive(false);
        }

        public void Tick(float deltaTime) {
            if (_hasEnded)
                return;

            if (_definition.UsesGravity == false)
                KeepStraight();

            _lifetimeLeft -= deltaTime;
            if (_lifetimeLeft <= 0f)
                End(_rigidbody.position);
        }

        private void KeepStraight() {
            if (_straightVelocity.sqrMagnitude < MIN_STRAIGHT_SPEED * MIN_STRAIGHT_SPEED)
                return;

            _rigidbody.linearVelocity = _straightVelocity;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        private void OnCollisionEnter(Collision collision) {
            if (_hasEnded || _definition == null)
                return;

            bool isBody = collision.rigidbody != null && collision.rigidbody.isKinematic == false;
            if (_definition.ExplodesOnImpact || (_definition.ExplodesOnBodyImpact && isBody))
                End(collision.contactCount > 0 ? collision.GetContact(0).point : _rigidbody.position);
        }

        private void End(Vector3 point) {
            _hasEnded = true;
            _rigidbody.linearVelocity = Vector3.zero;
            Ended?.Invoke(this, point);
        }

        private void IgnoreCollisions(IReadOnlyList<Collider> ignored) {
            RestoreCollisions();
            if (ignored == null)
                return;

            for (int i = 0; i < ignored.Count; i++) {
                Collider other = ignored[i];
                if (other == null || other == _collider)
                    continue;

                Physics.IgnoreCollision(_collider, other, true);
                _ignored.Add(other);
            }
        }

        // A collider may be gone (its player left) by the time the projectile goes back to the pool.
        private void RestoreCollisions() {
            for (int i = 0; i < _ignored.Count; i++)
                if (_ignored[i] != null)
                    Physics.IgnoreCollision(_collider, _ignored[i], false);

            _ignored.Clear();
        }
    }
}
