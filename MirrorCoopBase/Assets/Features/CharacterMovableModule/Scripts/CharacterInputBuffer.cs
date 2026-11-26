using System;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GameCoreModule.Contracts;
using Features.InputModule.Realization.Scripts.Generated;
using UnityEngine;
using Zenject;

namespace Features.CharacterMovableModule.Scripts {
    public sealed class CharacterInputBuffer : IInitializable, IDisposable, IGameplaySession {
        private readonly IInputService _inputService;
        private readonly CharacterMovableModel _model;
        private readonly PlayerControlBlockModel _controlBlock;
        private bool _scripted;
        // What the keyboard holds right now, also while blocked: a key held through a respawn or an unblock moves the
        // player again without a new press, because the input system sends no event for a key that stays down.
        private Vector2 _heldMove;
        private bool _heldSprint;

        public Vector2 MoveStick { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool IsBlocked => _controlBlock.IsBlocked;

        public void BeginScripted() {
            _scripted = true;
            ResetInput();
        }

        public void EndScripted() {
            _scripted = false;
            ResetInput();
        }

        public void SetScripted(Vector2 move, bool jump, bool sprint) {
            MoveStick = move;
            JumpHeld = jump;
            SprintHeld = sprint;
        }

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
            _scripted = false;
            ResetInput();
            _model.Clear();
        }

        public void RestartGameplay() {
            _scripted = false;
            ResetInput();
        }

        private void ResetInput() {
            MoveStick = Vector2.zero;
            JumpHeld = false;
            SprintHeld = false;
        }

        private void RestoreHeldInput() {
            if (_scripted)
                return;

            MoveStick = _heldMove;
            SprintHeld = _heldSprint;
        }

        private void OnControlBlockChanged() {
            if (_controlBlock.IsBlocked)
                ResetInput();
            else
                RestoreHeldInput();
        }

        private void OnMoveChanged(Vector2 value) {
            _heldMove = value;
            if (_scripted || _controlBlock.IsBlocked)
                return;

            MoveStick = value;
        }

        private void OnJumpStarted() {
            if (_scripted || _controlBlock.IsBlocked)
                return;

            JumpHeld = true;
        }

        private void OnJumpCanceled() {
            if (_scripted)
                return;

            JumpHeld = false;
        }

        private void OnSprintStarted() {
            _heldSprint = true;
            if (_scripted || _controlBlock.IsBlocked)
                return;

            SprintHeld = true;
        }

        private void OnSprintCanceled() {
            _heldSprint = false;
            if (_scripted)
                return;

            SprintHeld = false;
        }
    }
}
