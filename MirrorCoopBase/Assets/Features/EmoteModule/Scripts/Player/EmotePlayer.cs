using Features.CharacterMovableModule.Scripts;
using Features.EmoteModule.Contracts;
using Features.EmoteModule.Data;
using Features.GameFlowStateMachineModule.Scripts;
using Features.PlayerAnimationModule;
using UnityEngine;
using Zenject;

namespace Features.EmoteModule.Player {
    // Plays a player's emotes on this peer's copy of the body, on every peer that simulates it (the owner and the
    // copies of the others), from the emote plays it reads there (the owner's wheel, a copy's packets), through the
    // animation driver. An emote stops at once when the body starts to move (a walk input above the idle cutoff, a
    // jump, the body in the air), when it is held (a seat), or when the plays stop being readable here (a copy no
    // longer simulated). A copy also stops as soon as the owner's packets say the owner's emote no longer plays, so it
    // never keeps an emote the owner stopped, whatever it missed. The owner's emote stops on any game flow change
    // (a scene change, the lobby, the menu), so nobody carries a dance into the next scene.
    public sealed class EmotePlayer : MonoBehaviour {
        [SerializeField] private PlayerAnimationDriver _driver;
        [SerializeField] private CharacterMovableBase _movable;
        [Tooltip("Where the plays come from on this peer: a component that implements IEmotePlays.")]
        [SerializeField] private MonoBehaviour _plays;

        private readonly EmotePlayCounter _playCounter = new();

        private EmoteCatalog _catalog;
        private IGameFlowStateMachineService _gameFlow;
        private IEmotePlays _playSource;
        private int[] _triggers = new int[0];
        private int _playingTrigger;
        private bool _isPlaying;
        private byte _lastJumpCount;

        // Whether an emote may start now: the animator runs and the body stands still on its feet, not held.
        public bool CanStart =>
            _driver.CanPlay && _driver.IsWalking == false && _driver.IsFlying == false && _movable.IsHeld == false;

        // Whether an emote plays on this body (set on a play, cleared on a stop); the owner sends it to the copies.
        public bool IsPlaying => _isPlaying;

        [Inject]
        private void Construct(EmoteCatalog catalog, IGameFlowStateMachineService gameFlow) {
            _catalog = catalog;
            _gameFlow = gameFlow;
            _gameFlow.StateExited += HandleGameFlowStateExited;
            _triggers = new int[catalog.Emotes.Count];
            for (int i = 0; i < _triggers.Length; i++)
                _triggers[i] = Animator.StringToHash(catalog.Emotes[i].TriggerName);
        }

        private void Awake() {
            _playSource = _plays as IEmotePlays;
            if (_playSource == null)
                Debug.LogError($"Emote: {name} needs an IEmotePlays; it never emotes.", this);
        }

        private void OnDestroy() {
            if (_gameFlow != null)
                _gameFlow.StateExited -= HandleGameFlowStateExited;
        }

        private void HandleGameFlowStateExited(System.Type _) {
            if (_isPlaying && _movable.isOwned)
                Stop();
        }

        private void Update() {
            bool hasJumped = _movable.JumpCount != _lastJumpCount;
            _lastJumpCount = _movable.JumpCount;

            bool isAvailable = _playSource != null && _playSource.IsAvailable && _driver.CanPlay;
            bool isOwner = _movable.isOwned;
            if (_isPlaying && (isAvailable == false || hasJumped || _driver.IsWalking || _driver.IsFlying
                || _movable.IsHeld || (isOwner == false && _playSource.IsEmoting == false)))
                Stop();

            if (_playSource != null && _playCounter.Step(isAvailable, _playSource.EmoteCount))
                Play(_playSource.EmoteIndex);
        }

        private void Play(int index) {
            if (index < 0 || index >= _triggers.Length) {
                Debug.LogError($"Emote: {name} got emote {index}, the catalog has {_triggers.Length}.", this);
                return;
            }

            _driver.ResetTrigger(PlayerAnimatorParameters.EmoteStop);
            _driver.Play(_triggers[index]);
            _playingTrigger = _triggers[index];
            _isPlaying = true;
        }

        // A trigger not taken yet (the emote set this frame) is taken back too, so it does not start after the stop.
        // With the animator off nothing is sent to it: an animator turned off goes back to its default states.
        private void Stop() {
            _isPlaying = false;
            if (_driver.CanPlay == false)
                return;

            _driver.ResetTrigger(_playingTrigger);
            _driver.Play(PlayerAnimatorParameters.EmoteStop);
        }
    }
}
