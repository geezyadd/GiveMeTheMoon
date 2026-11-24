using Features.CameraModule.Scripts;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using UnityEngine;
using UnityEngine.Rendering;
using Zenject;

namespace Features.PlayerBodyModule {
    // Hides the player's own model (the body mesh and the face parts) while the player looks through the first-person
    // camera: those renderers then only cast shadows. In the third-person view, and for everyone else, the whole
    // body is drawn.
    public sealed class OwnBodyHider : MonoBehaviour {
        [SerializeField] private CharacterMovableBase _movable;
        [SerializeField] private Renderer[] _renderers = new Renderer[0];

        private IGameCameraService _cameras;
        private bool _isHidden;

        [Inject]
        private void Construct(IGameCameraService cameras) =>
            _cameras = cameras;

        private void LateUpdate() {
            bool hide = _movable != null && _movable.isLocalPlayer && _cameras != null
                        && _cameras.ActiveId == CameraIds.FPCamera;
            if (hide == _isHidden)
                return;

            ShadowCastingMode mode = hide ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
            foreach (Renderer modelRenderer in _renderers)
                if (modelRenderer != null)
                    modelRenderer.shadowCastingMode = mode;
            _isHidden = hide;
        }
    }
}
