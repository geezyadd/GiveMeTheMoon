using Features.CameraModule.Scripts;
using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using Features.WeaponModule.Scripts.Parts.Holding;
using UnityEngine;

namespace Features.HandWeaponModule.Scripts {
    // Keeps a held hand weapon where its holder and everyone else should see it: in the local holder's first-person
    // view it is placed right after the look is applied, so it turns with the view on the same frame; otherwise (the
    // holder in third person, every other peer) it is held out at the arm along the aim. A weapon let go of gets its
    // model back on the item.
    public sealed class HandWeaponPoseDriver {
        private readonly IGameCameraService _gameCameraService;
        private readonly GameCameraModel _gameCameraModel;
        private readonly HandWeaponPose _pose = new HandWeaponPose();

        private HandWeaponItem _item;
        private Transform _arm;
        private IWeaponAim _aim;
        private bool _isOwner;

        private bool IsInFirstPersonView =>
            _isOwner && _gameCameraModel.ActiveId == CameraIds.FPCamera && _gameCameraModel.FixedViewEye == null && _gameCameraModel.LookPivot != null;

        public HandWeaponPoseDriver(IGameCameraService gameCameraService, GameCameraModel gameCameraModel) {
            _gameCameraService = gameCameraService;
            _gameCameraModel = gameCameraModel;
        }

        public void Begin(HandWeaponItem item, Transform arm, IWeaponAim aim, bool isOwner) {
            _item = item;
            _arm = arm;
            _aim = aim;
            _isOwner = isOwner;
            if (isOwner)
                _gameCameraService.LookApplied += OnLookApplied;
        }

        // The item may already be gone (installed, destroyed with its scene) when the hand lets go.
        public void End() {
            if (_isOwner)
                _gameCameraService.LookApplied -= OnLookApplied;

            if (_item != null)
                _item.ResetPose();

            _isOwner = false;
            _item = null;
            _arm = null;
            _aim = null;
        }

        public void Advance() {
            if (IsInFirstPersonView == false)
                _item.SetPose(_pose.InHand(_arm, _aim.Direction, _item.Definition.HandOffset));
        }

        // The item may be gone between the hand's update and the camera's.
        private void OnLookApplied() {
            if (_item != null && IsInFirstPersonView)
                _item.SetPose(_pose.InView(_arm, _gameCameraModel.LookPivot, _item.Definition.ViewOffset));
        }
    }
}
