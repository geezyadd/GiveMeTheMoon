using System;

namespace Features.CameraModule.Scripts.Models {
    // The first-person camera's vertical field of view in degrees; the third-person camera keeps the catalog's offset
    // from it. Set from the player's settings, applied to the cameras by CameraFieldOfViewSystem.
    public sealed class CameraFieldOfViewModel {
        private float _firstPersonFieldOfView;

        public event Action OnChanged;

        public bool IsSet { get; private set; }

        public float FirstPersonFieldOfView {
            get => _firstPersonFieldOfView;
            set {
                if (IsSet && Math.Abs(_firstPersonFieldOfView - value) < 0.0001f)
                    return;

                _firstPersonFieldOfView = value;
                IsSet = true;
                OnChanged?.Invoke();
            }
        }
    }
}
