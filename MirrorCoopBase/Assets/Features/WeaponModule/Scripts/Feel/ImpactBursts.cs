using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // A flash, sparks, chips and a puff of dust where a shot struck, emitted into shared particle systems that simulate
    // in the floor's frame, so the bursts stay on a target the ship carries along.
    public sealed class ImpactBursts {
        private readonly ParticleSystem _flash;
        private readonly ParticleSystem _sparks;
        private readonly ParticleSystem _debris;
        private readonly ParticleSystem _dust;
        private readonly Transform _frame;
        private readonly WeaponFeelProfile _profile;

        private ParticleSystem.EmitParams _emitParams;

        public ImpactBursts(ParticleSystem flash, ParticleSystem sparks, ParticleSystem debris, ParticleSystem dust, Transform frame, WeaponFeelProfile profile) {
            _flash = flash;
            _sparks = sparks;
            _debris = debris;
            _dust = dust;
            _frame = frame;
            _profile = profile;
            SimulateInFrame(flash);
            SimulateInFrame(sparks);
            SimulateInFrame(debris);
            SimulateInFrame(dust);
        }

        public void Emit(Vector3 worldPoint, Vector3 worldNormal) {
            Vector3 point = _frame.InverseTransformPoint(worldPoint);
            Vector3 normal = _frame.InverseTransformDirection(worldNormal);
            if (normal.sqrMagnitude < Mathf.Epsilon)
                normal = Vector3.up;

            EmitFlash(point);
            EmitSpray(_sparks, _profile.ImpactSparkCount, _profile.ImpactSparkSpeed, point, normal.normalized);
            EmitSpray(_debris, _profile.ImpactDebrisCount, _profile.ImpactDebrisSpeed, point, normal.normalized);
            EmitDust(point, normal.normalized);
        }

        private void EmitSpray(ParticleSystem system, int count, float speed, Vector3 point, Vector3 normal) {
            _emitParams = default;
            _emitParams.position = point;
            for (int i = 0; i < count; i++) {
                Vector3 direction = (normal + Random.insideUnitSphere * _profile.ImpactSprayLean).normalized;
                _emitParams.velocity = direction * (speed * Random.Range(_profile.ImpactMinSpeedShare, 1f));
                system.Emit(_emitParams, 1);
            }
        }

        private void EmitFlash(Vector3 point) {
            _emitParams = default;
            _emitParams.position = point;
            _emitParams.startSize = _profile.ImpactFlashSizeMeters * Random.Range(_profile.ImpactFlashMinSizeShare, 1f);
            _emitParams.startLifetime = _profile.ImpactFlashSeconds;
            _emitParams.rotation = Random.Range(0f, 360f);
            _flash.Emit(_emitParams, 1);
        }

        private void EmitDust(Vector3 point, Vector3 normal) {
            _emitParams = default;
            _emitParams.position = point;
            _emitParams.velocity = normal * _profile.ImpactDustDriftSpeed;
            _emitParams.startSize = _profile.ImpactDustSizeMeters;
            _emitParams.rotation = Random.Range(0f, 360f);
            _dust.Emit(_emitParams, 1);
        }

        private void SimulateInFrame(ParticleSystem system) {
            ParticleSystem.MainModule main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Custom;
            main.customSimulationSpace = _frame;
        }
    }
}
