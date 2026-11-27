using Features.CameraModule.Scripts.Services;
using Features.ExplosionModule.Configuration;
using Features.ScreenShakeModule;
using Features.VfxModule;
using UnityEngine;

namespace Features.ExplosionModule {
    // Plays the preset's effect at the centre and shakes this peer's camera, fully at the centre and less with the
    // camera's distance, to nothing at the preset's shake distance. An explosion that went off on wide flat ground
    // (ExplosionGroundProbe) plays the preset's ground effect on the surface instead of the air one.
    public sealed class ExplosionEffects : IExplosionEffects {
        private readonly IVfxService _vfxService;
        private readonly IScreenShakeService _screenShakeService;
        private readonly IGameCameraService _gameCameraService;
        private readonly ExplosionConfiguration _configuration;
        private readonly ExplosionGroundProbe _groundProbe = new();

        public ExplosionEffects(IVfxService vfxService, IScreenShakeService screenShakeService, IGameCameraService gameCameraService,
                                ExplosionConfiguration configuration) {
            _vfxService = vfxService;
            _screenShakeService = screenShakeService;
            _gameCameraService = gameCameraService;
            _configuration = configuration;
        }

        public void Play(Vector3 center, ExplosionPreset preset) {
            if (preset.GroundEffect != null
                && _groundProbe.TryFindGround(center, preset.GroundFlatRadius, _configuration.Ground, out Vector3 groundPoint))
                _vfxService.Play(preset.GroundEffect, groundPoint, Quaternion.identity, preset.GroundEffectScale);
            else if (preset.Effect != null)
                _vfxService.Play(preset.Effect, center, Quaternion.identity, preset.EffectScale);

            if (preset.Shake != null)
                _screenShakeService.Shake(preset.Shake, ShakeStrength(center, preset));
        }

        private float ShakeStrength(Vector3 center, ExplosionPreset preset) {
            Camera camera = _gameCameraService.OutputCamera;
            if (camera == null)
                return 0f;

            float distance = Vector3.Distance(camera.transform.position, center);
            return Mathf.Clamp01(1f - distance / preset.ShakeDistance);
        }
    }
}
