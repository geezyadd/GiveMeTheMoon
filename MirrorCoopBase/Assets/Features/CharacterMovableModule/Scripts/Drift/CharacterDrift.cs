using Features.CameraModule.Contracts;
using Features.CharacterMovableModule.Scripts.Body;
using UnityEngine;
using Zenject;

namespace Features.CharacterMovableModule.Scripts.Drift {
    // A player's body drifting in open space, on every peer that simulates it. The body the movement drives (the
    // ragdoll, or the capsule) is weightless and the movement neither walks nor jumps; each physics step the velocity
    // fades to a slow drift (CharacterDriftMotion). On the owner's peer the move keys also push it a little along the
    // view, but not while the body lies limp in a knockdown: then it only drifts. A copy of someone else's body only drifts: the owner's push reaches it through the packets' velocity.
    // Who starts and ends a drift is the ship's overboard rule.
    public sealed class CharacterDrift : MonoBehaviour {
        [SerializeField] private CharacterMovableBase _movable;

        private readonly CharacterDriftMotion _motion = new CharacterDriftMotion();

        private CharacterInputBuffer _characterInputBuffer;
        private ILocalViewDirectionSource _localViewDirectionSource;
        private CharacterDriftSettings _settings;
        private DriftPushScaleModel _driftPushScaleModel;

        public bool IsDrifting => _movable.IsDrifting;
        // The body that drifts now: the one the movement drives.
        public IMovableBody Body => _movable.DrivenBody;
        public bool IsOwnBody => _movable.isOwned;
        // The numbers of the last drift (null before the first one).
        public CharacterDriftSettings Settings => _settings;

        [Inject]
        private void InjectDependencies(
            CharacterInputBuffer characterInputBuffer,
            ILocalViewDirectionSource localViewDirectionSource,
            DriftPushScaleModel driftPushScaleModel) {
            _characterInputBuffer = characterInputBuffer;
            _localViewDirectionSource = localViewDirectionSource;
            _driftPushScaleModel = driftPushScaleModel;
        }

        private void OnDisable() =>
            End();

        private void FixedUpdate() {
            // Nobody simulates the body on this peer (a copy before its first packet) or something holds it.
            if (IsDrifting == false || _movable.ActiveControls == null)
                return;

            IMovableBody body = _movable.DrivenBody;
            if (body.IsKinematic)
                return;

            Vector3 push = _movable.isOwned && _movable.IsKnockedDown == false ? ReadPushDirection() : Vector3.zero;
            body.SetVelocity(_motion.Step(body.Velocity, push, _settings, Time.fixedDeltaTime, _driftPushScaleModel.Scale));
        }

        // The body keeps the velocity it has at this moment (the exit velocity in the world).
        public void Begin(CharacterDriftSettings settings) {
            if (IsDrifting)
                return;

            _settings = settings;
            _movable.SetDrifting(true);
        }

        public void End() {
            if (IsDrifting)
                _movable.SetDrifting(false);
        }

        private Vector3 ReadPushDirection() {
            if (_characterInputBuffer.IsBlocked)
                return Vector3.zero;

            Vector2 stick = _characterInputBuffer.MoveStick;
            return _localViewDirectionSource.Forward * stick.y + _localViewDirectionSource.Right * stick.x;
        }
    }
}
