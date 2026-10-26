using UnityEngine;

namespace Features.GrabModule.Scripts.Hold {
    // Server: one player's hold on an interactable. It completes after the hold time and resets the moment the
    // target stops being usable, the player leaves reach or another item is in the hand.
    public sealed class HoldUseProgress {
        private float _seconds;
        private float _elapsed;
        private uint _heldItemNetId;

        public bool IsActive { get; private set; }
        public float Progress => IsActive ? Mathf.Clamp01(_elapsed / _seconds) : 0f;

        public void Begin(float seconds, uint heldItemNetId) {
            IsActive = true;
            _seconds = Mathf.Max(seconds, Mathf.Epsilon);
            _elapsed = 0f;
            _heldItemNetId = heldItemNetId;
        }

        public void Cancel() {
            IsActive = false;
            _elapsed = 0f;
        }

        public HoldUseStatus Advance(float deltaTime, HoldUseCheck check) {
            if (IsActive == false)
                return HoldUseStatus.None;

            if (IsBroken(check)) {
                Cancel();
                return HoldUseStatus.Cancelled;
            }

            _elapsed += deltaTime;
            if (_elapsed < _seconds)
                return HoldUseStatus.Holding;

            Cancel();
            return HoldUseStatus.Completed;
        }

        private bool IsBroken(HoldUseCheck check) =>
            check.IsTargetUsable == false || check.IsInReach == false || check.HeldItemNetId != _heldItemNetId;
    }
}
