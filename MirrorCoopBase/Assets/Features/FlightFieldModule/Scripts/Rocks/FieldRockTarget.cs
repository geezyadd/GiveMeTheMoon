using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Next to a field rock's mesh: the trigger a weapon ray strikes and the rock's weapon target id. The builders bind
    // it while the rock stands as a field rock and unbind it when the rock goes back to the pool; an unbound rock (a
    // pooled one, or a decor rock of the same prefab) has its collider off, so no ray finds it. The server never reads
    // it: a reported hit is checked against the field data.
    public sealed class FieldRockTarget : MonoBehaviour, IWeaponTargetCollider, IWeaponImpactFeedback {
        [SerializeField] private Collider _collider;
        [SerializeField] private MeshRenderer _renderer;

        private IFieldRockHitFeedback _hitFeedback;

        public ulong TargetId { get; private set; }
        public MeshRenderer Renderer => _renderer;

        public void Bind(ulong targetId, IFieldRockHitFeedback hitFeedback) {
            TargetId = targetId;
            _hitFeedback = hitFeedback;
            _collider.enabled = true;
        }

        public void Unbind() {
            _hitFeedback.Stop(this);
            _hitFeedback = null;
            TargetId = 0;
            _collider.enabled = false;
        }

        public void PresentImpact(Vector3 point, Vector3 normal) =>
            _hitFeedback.Play(this);
    }
}
