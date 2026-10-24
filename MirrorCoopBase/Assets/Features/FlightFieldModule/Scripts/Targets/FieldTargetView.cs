using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Targets {
    // The root of a target balloon prefab, authored for a sphere of radius 1 m: the trigger a weapon ray strikes (layer
    // FieldRock) and its weapon target id, and the look that bobs gently and squashes when struck. Only the visual
    // child moves; the root stays where the field data puts the balloon. The builder binds it while it stands in the
    // field and unbinds it when it goes back to the pool (collider off, look reset). The server never reads it: a
    // reported hit is checked against the field data.
    public sealed class FieldTargetView : MonoBehaviour, IWeaponTargetCollider, IWeaponImpactFeedback {
        private const float FULL_TURN = Mathf.PI * 2f;

        [SerializeField] private Collider _collider;
        [SerializeField] private Transform _visual;

        private float _bobAmplitude;
        private float _bobAngularSpeed;
        private float _bobPhase;
        private float _squashSeconds;
        private float _squashShare;
        private float _squashSecondsLeft;

        public ulong TargetId { get; private set; }

        private void Update() {
            float squash = _squashSecondsLeft > 0f ? _squashShare * (_squashSecondsLeft / _squashSeconds) : 0f;
            _squashSecondsLeft = Mathf.Max(0f, _squashSecondsLeft - Time.deltaTime);
            _visual.localPosition = Vector3.up * (_bobAmplitude * Mathf.Sin(Time.time * _bobAngularSpeed + _bobPhase));
            _visual.localScale = new Vector3(1f + squash, 1f - squash, 1f + squash);
        }

        // The bob amplitude is in meters of the world; the root is scaled by the balloon's radius.
        public void Bind(ulong targetId, float radius, float bobAmplitudeMeters, float bobPeriodSeconds, float bobPhase, float squashSeconds, float squashShare) {
            TargetId = targetId;
            _bobAmplitude = bobAmplitudeMeters / radius;
            _bobAngularSpeed = FULL_TURN / bobPeriodSeconds;
            _bobPhase = bobPhase;
            _squashSeconds = squashSeconds;
            _squashShare = squashShare;
            _squashSecondsLeft = 0f;
            _collider.enabled = true;
        }

        public void Unbind() {
            TargetId = 0;
            _collider.enabled = false;
            _squashSecondsLeft = 0f;
            _visual.localPosition = Vector3.zero;
            _visual.localScale = Vector3.one;
        }

        public void PresentImpact(Vector3 point, Vector3 normal) =>
            _squashSecondsLeft = _squashSeconds;
    }
}
