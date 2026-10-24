using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;
using Zenject;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Every peer: a struck rock shakes and flashes for a moment, fading out; another hit restarts it, so a rock under a
    // stream keeps trembling. The flash is a property block on the rock only while it runs: the rock drops it at rest
    // and batches with the others again. The chips and dust at the impact are the weapon's own impact feel.
    public sealed class FieldRockHitFeedbackSystem : IFieldRockHitFeedback, ITickable {
        private static readonly int _emissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly FieldRockDurabilityConfiguration _fieldRockDurabilityConfiguration;
        private readonly List<FieldRockHitShake> _shakes = new();
        private readonly MaterialPropertyBlock _flash = new();

        public FieldRockHitFeedbackSystem(FieldRockDurabilityConfiguration fieldRockDurabilityConfiguration) =>
            _fieldRockDurabilityConfiguration = fieldRockDurabilityConfiguration;

        public void Tick() {
            for (int i = _shakes.Count - 1; i >= 0; i--) {
                FieldRockHitShake shake = _shakes[i];
                shake.SecondsLeft -= Time.deltaTime;
                if (shake.SecondsLeft <= 0f) {
                    PutAtRest(shake);
                    _shakes.RemoveAt(i);
                    continue;
                }

                _shakes[i] = shake;
                Present(shake);
            }
        }

        public void Play(FieldRockTarget target) {
            int index = IndexOf(target);
            if (index >= 0) {
                FieldRockHitShake running = _shakes[index];
                running.SecondsLeft = _fieldRockDurabilityConfiguration.HitFeedbackSeconds;
                _shakes[index] = running;
                return;
            }

            _shakes.Add(new FieldRockHitShake(target, target.transform.localPosition, _fieldRockDurabilityConfiguration.HitFeedbackSeconds));
        }

        public void Stop(FieldRockTarget target) {
            int index = IndexOf(target);
            if (index < 0)
                return;

            PutAtRest(_shakes[index]);
            _shakes.RemoveAt(index);
        }

        // The shake is along a random direction in the rock's own space, so it reads the same at any rock scale.
        private void Present(in FieldRockHitShake shake) {
            float strength = shake.SecondsLeft / _fieldRockDurabilityConfiguration.HitFeedbackSeconds;
            Transform mesh = shake.Target.transform;
            mesh.localPosition = shake.RestLocalPosition + Random.insideUnitSphere * (_fieldRockDurabilityConfiguration.HitShakeShare * strength);
            MeshRenderer renderer = shake.Target.Renderer;
            Color restEmission = renderer.sharedMaterial.GetColor(_emissionColorId);
            _flash.SetColor(_emissionColorId, restEmission + _fieldRockDurabilityConfiguration.HitFlashEmission * strength);
            renderer.SetPropertyBlock(_flash);
        }

        // A rock returned to the pool or destroyed with its scene is left alone.
        private static void PutAtRest(in FieldRockHitShake shake) {
            if (shake.Target == null)
                return;

            shake.Target.transform.localPosition = shake.RestLocalPosition;
            shake.Target.Renderer.SetPropertyBlock(null);
        }

        private int IndexOf(FieldRockTarget target) {
            for (int i = 0; i < _shakes.Count; i++) {
                if (_shakes[i].Target == target)
                    return i;
            }

            return -1;
        }
    }
}
