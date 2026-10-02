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
        private bool _scripted;

        public Vector2 MoveStick { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool SprintHeld { get; private set; }

        public void BeginScripted() {
            _scripted = true;
            MoveStick = Vector2.zero;
            JumpHeld = false;
            SprintHeld = false;
        }

        public void EndScripted() {
            _scripted = false;
            MoveStick = Vector2.zero;
            JumpHeld = false;
            SprintHeld = false;
        }

        public void SetScripted(Vector2 move, bool jump, bool sprint) {
            MoveStick = move;
            JumpHeld = jump;
            SprintHeld = sprint;
        }

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
            _scripted = false;
            MoveStick = Vector2.zero;
            JumpHeld = false;
            SprintHeld = false;
            _model.Clear();
        }

        public void RestartGameplay() {
            _scripted = false;
            MoveStick = Vector2.zero;
            JumpHeld = false;
            SprintHeld = false;
        }

        private void OnMoveChanged(Vector2 value) {
            if (_scripted)
                return;

            MoveStick = value;
        }

        private void OnJumpStarted() {
            if (_scripted)
                return;

            JumpHeld = true;
        }

        private void OnJumpCanceled() {
            if (_scripted)
                return;

            JumpHeld = false;
        }

        private void OnSprintStarted() {
            if (_scripted)
                return;

            SprintHeld = true;
        }

        private void OnSprintCanceled() {
            if (_scripted)
                return;

            SprintHeld = false;
        }
    }
}
