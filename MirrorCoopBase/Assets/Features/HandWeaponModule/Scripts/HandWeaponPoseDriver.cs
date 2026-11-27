using Features.CameraModule.Scripts.Services;
using Features.WeaponModule.Scripts.Parts.Holding;
using UnityEngine;

namespace Features.HandWeaponModule.Scripts {
    // Keeps a held hand weapon in its holder's real right hand, the same for the holder in either view and for every
    // other peer: the grip in the palm (HandWeaponGrip), the barrel along the aim, while the arm reaches along it. On the
    // holder's own peer it is placed right after the look is applied, so it turns with the view on the same frame. A
    // weapon let go of gets its model back on the item.
    public sealed class HandWeaponPoseDriver {
        private readonly IGameCameraService _gameCameraService;
        private readonly HandWeaponGrip _grip;
        private readonly HandWeaponPose _pose = new HandWeaponPose();

        private HandWeaponItem _item;
        private Transform _arm;
        private IWeaponAim _aim;
        private bool _isOwner;

        public HandWeaponPoseDriver(IGameCameraService gameCameraService, HandWeaponGrip grip) {
            _gameCameraService = gameCameraService;
            _grip = grip;
        }

        public void Begin(HandWeaponItem item, Transform arm, IWeaponAim aim, bool isOwner) {
            _item = item;
            _arm = arm;
            _aim = aim;
            _isOwner = isOwner;
            _grip.Begin(aim);
            if (isOwner)
                _gameCameraService.LookApplied += OnLookApplied;
        }

        // The item may already be gone (installed, destroyed with its scene) when the hand lets go.
        public void End() {
            if (_isOwner)
                _gameCameraService.LookApplied -= OnLookApplied;

            _grip.End();
            if (_item != null)
                _item.ResetPose();

            _isOwner = false;
            _item = null;
            _arm = null;
            _aim = null;
        }

        public void Advance() {
            if (_isOwner == false)
                Apply();
        }

        // The item may be gone between the hand's update and the camera's.
        private void OnLookApplied() {
            if (_item != null)
                Apply();
        }

        private void Apply() =>
            _item.SetPose(_pose.AtGrip(_arm, _grip.GripPoint, _aim.Direction, _item.HandDefinition.GripOffset));
    }
}
