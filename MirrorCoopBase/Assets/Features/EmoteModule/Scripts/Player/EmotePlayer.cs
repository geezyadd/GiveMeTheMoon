using Features.CharacterMovableModule.Scripts;
using Features.EmoteModule.Contracts;
using Features.EmoteModule.Data;
using Features.PlayerAnimationModule;
using UnityEngine;
using Zenject;

namespace Features.EmoteModule.Player {
    // Plays a player's emotes on this peer's copy of the body, on every peer that simulates it (the owner and the
    // copies of the others), from the emote plays it reads there (the owner's wheel, a copy's packets), through the
    // animation driver. An emote stops at once when the body starts to move: a walk input above the idle cutoff, a
    // jump, or the body in the air (Fly). Every peer sees that from its own controls (a copy's come from the owner's
    // packets), so a stop needs no message of its own.
    public sealed class EmotePlayer : MonoBehaviour {
        [SerializeField] private PlayerAnimationDriver _driver;
        [SerializeField] private CharacterMovableBase _movable;
        [Tooltip("Where the plays come from on this peer: a component that implements IEmotePlays.")]
        [SerializeField] private MonoBehaviour _plays;

        private readonly EmotePlayCounter _playCounter = new();

        private EmoteCatalog _catalog;
        private IEmotePlays _playSource;
        private int[] _triggers = new int[0];
        private int _playingTrigger;
        private bool _isPlaying;
        private byte _lastJumpCount;

        // Whether an emote may start now: the animator runs and the body stands still on its feet.
        public bool CanStart => _driver.CanPlay && _driver.IsWalking == false && _driver.IsFlying == false;

        [Inject]
        private void Construct(EmoteCatalog catalog) {
            _catalog = catalog;
            _triggers = new int[catalog.Emotes.Count];
            for (int i = 0; i < _triggers.Length; i++)
                _triggers[i] = Animator.StringToHash(catalog.Emotes[i].TriggerName);
        }

        private void Awake() {
            _playSource = _plays as IEmotePlays;
            if (_playSource == null)
                Debug.LogError($"Emote: {name} needs an IEmotePlays; it never emotes.", this);
        }

        private void Update() {
            bool hasJumped = _movable.JumpCount != _lastJumpCount;
            _lastJumpCount = _movable.JumpCount;

            bool isAvailable = _playSource != null && _playSource.IsAvailable && _driver.CanPlay;
            if (isAvailable == false)
                _isPlaying = false;

            if (_isPlaying && (hasJumped || _driver.IsWalking || _driver.IsFlying))
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
        private void Stop() {
            _driver.ResetTrigger(_playingTrigger);
            _driver.Play(PlayerAnimatorParameters.EmoteStop);
            _isPlaying = false;
        }
    }
}
