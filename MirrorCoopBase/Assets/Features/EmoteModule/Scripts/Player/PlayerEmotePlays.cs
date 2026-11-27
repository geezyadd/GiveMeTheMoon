using Features.CharacterMovableModule.Scripts;
using Features.EmoteModule.Contracts;
using Features.PlayerReplicaModule;
using UnityEngine;
using Zenject;

namespace Features.EmoteModule.Player {
    // A player's emote plays on this peer (IEmotePlays). On the owner's peer: the emotes picked on the local emote
    // wheel, counted only when one may start (EmotePlayer.CanStart: the animator runs, the body stands still), so a
    // refused pick never reaches the copies. On another peer: the owner's count and emote in the packet the copy shows,
    // while the copy is simulated. The body state packet carries both to the copies.
    public sealed class PlayerEmotePlays : MonoBehaviour, IEmotePlays {
        [SerializeField] private CharacterMovableBase _movable;
        [SerializeField] private BodyCopyDriver _copy;
        [SerializeField] private EmotePlayer _player;

        private EmoteWheelModel _wheelModel;
        private byte _ownCount;
        private byte _ownIndex;

        public bool IsAvailable => _movable.isOwned || _copy.IsSimulating;
        public byte EmoteCount => _movable.isOwned ? _ownCount : _copy.ShownEmoteCount;
        public byte EmoteIndex => _movable.isOwned ? _ownIndex : _copy.ShownEmoteIndex;

        [Inject]
        private void Construct(EmoteWheelModel wheelModel) {
            _wheelModel = wheelModel;
            _wheelModel.OnEmoteChosen += HandleEmoteChosen;
        }

        private void OnDestroy() {
            if (_wheelModel != null)
                _wheelModel.OnEmoteChosen -= HandleEmoteChosen;
        }

        private void HandleEmoteChosen(int index) {
            if (_movable.isOwned == false || _player.CanStart == false || index < 0 || index > byte.MaxValue)
                return;

            _ownIndex = (byte)index;
            _ownCount = unchecked((byte)(_ownCount + 1));
        }
    }
}
