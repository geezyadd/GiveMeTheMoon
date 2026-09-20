using System;
using Features.CameraModule.Scripts.Models;
using Features.GameCoreModule.Scripts;
using Features.InputModule.Realization.Scripts.Generated;
using UnityEngine;
using Zenject;

namespace Features.CameraModule.Scripts {
    public sealed class CameraLookDriver : ITickable, IInitializable, IDisposable, IGameplaySession {
        private readonly IInputService _input;
        private readonly GameCameraModel _model;
        private readonly CameraCatalog _catalog;

        private CameraLookRig _rig;
        private Vector2 _pendingLook;
        private float _yaw;
        private float _pitch;
        private bool _synced;
        private bool _cursorLocked;
        private bool _cursorWantedLocked = true;

        public CameraLookDriver(IInputService input, GameCameraModel model, CameraCatalog catalog) {
            _input = input;
            _model = model;
            _catalog = catalog;
        }

        public void Initialize() {
            _input.Look.VectorChangedPerformed += OnLook;
            _input.Look.VectorChangedCanceled += OnLook;
            _input.ToggleCursor.Performed += OnToggleCursor;
        }

        public void Dispose() {
            _input.Look.VectorChangedPerformed -= OnLook;
            _input.Look.VectorChangedCanceled -= OnLook;
            _input.ToggleCursor.Performed -= OnToggleCursor;
            ResetLook();
        }

        public void CleanupGameplay() {
            ResetLook();
        }

        public void RestartGameplay() {
            ResetLook();
        }

        public void Tick() {
            CameraLookRig rig = EnsureRig();
            Transform follow = _model.Follow;
            if (follow == null) {
                _synced = false;
                _cursorWantedLocked = true;
                if (rig != null)
                    rig.Anchor = null;

                UnlockCursor();
                _pendingLook = Vector2.zero;
                return;
            }

            if (_synced == false) {
                _yaw = follow.eulerAngles.y;
                _pitch = 0f;
                _synced = true;
                _pendingLook = Vector2.zero;
            }

            if (_cursorWantedLocked == false) {
                UnlockCursor();
                _pendingLook = Vector2.zero;
                ApplyRig(rig, follow);
                return;
            }

            Vector2 look = _pendingLook;
            _pendingLook = Vector2.zero;
            if (look.sqrMagnitude > 0.000001f) {
                float sensitivity = _catalog != null ? _catalog.LookSensitivity : 0.18f;
                float invert = _catalog != null && _catalog.InvertY ? 1f : -1f;
                _yaw += look.x * sensitivity;
                _pitch += look.y * sensitivity * invert;

                float minPitch = _catalog != null ? _catalog.MinPitch : -70f;
                float maxPitch = _catalog != null ? _catalog.MaxPitch : 70f;
                _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
                if (_yaw > 180f || _yaw < -180f)
                    _yaw = Mathf.Repeat(_yaw + 180f, 360f) - 180f;
            }

            ApplyRig(rig, follow);
            LockCursor();
        }

        private void ApplyRig(CameraLookRig rig, Transform follow) {
            if (rig == null)
                return;

            rig.Anchor = _model.Eye != null ? _model.Eye : follow;
            rig.Yaw = _yaw;
            rig.Pitch = _pitch;
        }

        private CameraLookRig EnsureRig() {
            if (_rig != null)
                return _rig;

            Transform pivot = _model.LookPivot;
            if (pivot == null)
                return null;

            _rig = pivot.GetComponent<CameraLookRig>();
            if (_rig == null)
                _rig = pivot.gameObject.AddComponent<CameraLookRig>();

            return _rig;
        }

        private void ResetLook() {
            _rig = null;
            _pendingLook = Vector2.zero;
            _yaw = 0f;
            _pitch = 0f;
            _synced = false;
            _cursorWantedLocked = true;
            UnlockCursor(true);
        }

        private void OnLook(Vector2 value) {
            if (_cursorWantedLocked == false)
                return;

            _pendingLook += value;
        }

        private void OnToggleCursor() {
            _cursorWantedLocked = !_cursorWantedLocked;
            if (_cursorWantedLocked)
                LockCursor();
            else
                UnlockCursor();
        }

        private void LockCursor() {
            if (_cursorLocked)
                return;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _cursorLocked = true;
        }

        private void UnlockCursor(bool force = false) {
            if (force == false && _cursorLocked == false)
                return;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            _cursorLocked = false;
        }
    }
}
