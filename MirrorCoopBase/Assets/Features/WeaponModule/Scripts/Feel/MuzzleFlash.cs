using Features.WeaponModule.Scripts.Definitions;
using UnityEngine;

namespace Features.WeaponModule.Scripts.Feel {
    // A soft bright glow on the muzzle every frame of fire, flickering in size, and a short light with it.
    public sealed class MuzzleFlash {
        private readonly ParticleSystem _glow;
        private readonly Light _light;
        private readonly WeaponFeelProfile _profile;

        private ParticleSystem.EmitParams _emitParams;

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

        public void Present(bool isFiring) {
            _light.enabled = isFiring;
            if (isFiring == false)
                return;

            float flicker = 1f + Random.Range(-_profile.MuzzleFlashFlicker, _profile.MuzzleFlashFlicker);
            _light.intensity = _profile.MuzzleLightIntensity * flicker;
            _emitParams.startSize = _profile.MuzzleFlashSize * flicker;
            _emitParams.rotation = Random.Range(0f, 360f);
            _glow.Emit(_emitParams, 1);
        }
    }
}
