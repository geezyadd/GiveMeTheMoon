using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // A small short glow on the muzzle on every shot, flickering in size, and a short light with it. One flash per shot
    // (not per frame of fire), so the gunner still sees the stream of bullets past it.
    public sealed class MuzzleFlash {
        private readonly ParticleSystem _glow;
        private readonly Light _light;
        private readonly WeaponFeelProfile _profile;

        private ParticleSystem.EmitParams _emitParams;
        private float _lightSecondsLeft;

        public MuzzleFlash(ParticleSystem glow, Light light, WeaponFeelProfile profile) {
            _glow = glow;
            _light = light;
            _profile = profile;
            _emitParams.startColor = profile.MuzzleFlashColor;
            _emitParams.startLifetime = profile.MuzzleFlashSeconds;
            _emitParams.applyShapeToPosition = true;
            _light.color = profile.MuzzleFlashColor;
            _light.range = profile.MuzzleLightRange;
            _light.enabled = false;
        }

        public void Flash() {
            float flicker = 1f + Random.Range(-_profile.MuzzleFlashFlicker, _profile.MuzzleFlashFlicker);
            _light.intensity = _profile.MuzzleLightIntensity * flicker;
            _light.enabled = true;
            _lightSecondsLeft = _profile.MuzzleFlashSeconds;
            _emitParams.startSize = _profile.MuzzleFlashSize * flicker;
            _emitParams.rotation = Random.Range(0f, 360f);
            _glow.Emit(_emitParams, 1);
        }

        public void Advance(float deltaTime) {
            if (_light.enabled == false)
                return;

            _lightSecondsLeft -= deltaTime;
            if (_lightSecondsLeft <= 0f)
                _light.enabled = false;
        }
    }
}
