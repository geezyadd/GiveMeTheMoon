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
        private readonly PlayerControlBlockModel _controlBlock;

        public Vector2 MoveStick { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool IsBlocked => _controlBlock.IsBlocked;

        public CharacterInputBuffer(
            IInputService inputService,
            CharacterMovableModel model,
            PlayerControlBlockModel controlBlock) {
            _inputService = inputService;
            _model = model;
            _controlBlock = controlBlock;
        }

        public void Initialize() {
            _inputService.Movement.VectorChangedPerformed += OnMoveChanged;
            _inputService.Movement.VectorChangedCanceled += OnMoveChanged;
            _inputService.Jump.Started += OnJumpStarted;
            _inputService.Jump.Canceled += OnJumpCanceled;
            _inputService.Sprint.Started += OnSprintStarted;
            _inputService.Sprint.Canceled += OnSprintCanceled;
            _controlBlock.OnChanged += OnControlBlockChanged;
        }

        public void Dispose() {
            _inputService.Movement.VectorChangedPerformed -= OnMoveChanged;
            _inputService.Movement.VectorChangedCanceled -= OnMoveChanged;
            _inputService.Jump.Started -= OnJumpStarted;
            _inputService.Jump.Canceled -= OnJumpCanceled;
            _inputService.Sprint.Started -= OnSprintStarted;
            _inputService.Sprint.Canceled -= OnSprintCanceled;
            _controlBlock.OnChanged -= OnControlBlockChanged;
        }

        public void CleanupGameplay() {
            ResetInput();
            _model.Clear();
        }

        public void RestartGameplay() =>
            ResetInput();

        private void ResetInput() {
            MoveStick = Vector2.zero;
            JumpHeld = false;
            SprintHeld = false;
        }

        private void OnControlBlockChanged() {
            if (_controlBlock.IsBlocked)
                ResetInput();
        }

        private void OnMoveChanged(Vector2 value) {
            if (_controlBlock.IsBlocked)
                return;

            MoveStick = value;
        }

        private void OnJumpStarted() {
            if (_controlBlock.IsBlocked)
                return;

            JumpHeld = true;
        }

        private void OnJumpCanceled() {
            JumpHeld = false;
        }

        private void OnSprintStarted() {
            if (_controlBlock.IsBlocked)
                return;

            SprintHeld = true;
        }

        private void OnSprintCanceled() {
            SprintHeld = false;
        }
    }
}
