using System;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GameCoreModule.Scripts;
using Features.InputModule.Realization.Scripts.Generated;
using UnityEngine;
using Zenject;

namespace Features.CharacterMovableModule.Scripts {
    public sealed class CharacterInputBuffer : IInitializable, IDisposable, IGameplaySession {
        private readonly IInputService _inputService;
        private readonly CharacterMovableModel _model;
        private bool _suppressed;

        public Vector2 MoveStick { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool SprintHeld { get; private set; }

        public CharacterInputBuffer(IInputService inputService, CharacterMovableModel model) {
            _inputService = inputService;
            _model = model;
        }

        public void Initialize() {
            _inputService.Movement.VectorChangedPerformed += OnMoveChanged;
            _inputService.Movement.VectorChangedCanceled += OnMoveChanged;
            _inputService.Jump.Started += OnJumpStarted;
            _inputService.Jump.Canceled += OnJumpCanceled;
            _inputService.Sprint.Started += OnSprintStarted;
            _inputService.Sprint.Canceled += OnSprintCanceled;
        }

        public void Dispose() {
            _inputService.Movement.VectorChangedPerformed -= OnMoveChanged;
            _inputService.Movement.VectorChangedCanceled -= OnMoveChanged;
            _inputService.Jump.Started -= OnJumpStarted;
            _inputService.Jump.Canceled -= OnJumpCanceled;
            _inputService.Sprint.Started -= OnSprintStarted;
            _inputService.Sprint.Canceled -= OnSprintCanceled;
        }

        public void CleanupGameplay() {
            _suppressed = false;
            MoveStick = Vector2.zero;
            JumpHeld = false;
            SprintHeld = false;
            _model.Clear();
        }

        public void RestartGameplay() {
            _suppressed = false;
            MoveStick = Vector2.zero;
            JumpHeld = false;
            SprintHeld = false;
        }

        public void SetSuppressed(bool suppressed) {
            _suppressed = suppressed;
            if (suppressed == false)
                return;

            MoveStick = Vector2.zero;
            JumpHeld = false;
            SprintHeld = false;
        }

        private void OnMoveChanged(Vector2 value) {
            if (_suppressed)
                return;

            MoveStick = value;
        }

        private void OnJumpStarted() {
            if (_suppressed)
                return;

            JumpHeld = true;
        }

        private void OnJumpCanceled() {
            JumpHeld = false;
        }

        private void OnSprintStarted() {
            if (_suppressed)
                return;

            SprintHeld = true;
        }

        private void OnSprintCanceled() {
            SprintHeld = false;
        }
    }
}
