using Features.AttackModule;
using Features.AttackModule.Contracts;
using Features.CameraModule.Scripts.Models;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.Models;
using Features.GrabModule.Scripts;
using Features.InputModule.Realization.Scripts.Generated;
using Features.PlayerReplicaModule;
using UnityEngine;
using Zenject;

namespace Features.PlayerBodyModule {
    // A player's attack presses on this peer (IAttackPresses). On the owner's peer: the fire button, counted only when
    // the player may attack (PlayerAttackPressRule: an empty hand, not seated, the controls and the cursor free, no
    // attack running), so a refused press never reaches the copies. On another peer: the owner's count in the packet
    // the copy shows, while the copy is simulated. The body state packet carries the count to the copies.
    public sealed class PlayerAttackPresses : MonoBehaviour, IAttackPresses {
        [SerializeField] private CharacterMovableBase _movable;
        [SerializeField] private BodyCopyDriver _copy;
        [SerializeField] private GrabController _hand;
        [SerializeField] private Attacker _attacker;

        private IInputService _inputService;
        private PlayerControlBlockModel _controlBlock;
        private CursorModel _cursor;
        private byte _ownCount;

        public bool IsAvailable => _movable.isOwned || _copy.IsSimulating;
        public byte AttackCount => _movable.isOwned ? _ownCount : _copy.ShownAttackCount;

        [Inject]
        private void Construct(IInputService inputService, PlayerControlBlockModel controlBlock, CursorModel cursor) {
            _inputService = inputService;
            _controlBlock = controlBlock;
            _cursor = cursor;
            _inputService.Fire.Started += HandleFireStarted;
        }

        private void OnDestroy() {
            if (_inputService != null)
                _inputService.Fire.Started -= HandleFireStarted;
        }

        private void HandleFireStarted() {
            if (_movable.isOwned == false)
                return;

            var state = new PlayerAttackPressRule.State(
                isHolding: _hand.IsHolding,
                isSeated: _movable.IsHeld,
                isControlBlocked: _controlBlock.IsBlocked,
                isCursorFree: _cursor.IsFreeCursorRequested,
                isAttacking: _attacker.IsAttacking);
            if (PlayerAttackPressRule.Allows(state))
                _ownCount = unchecked((byte)(_ownCount + 1));
        }
    }
}
