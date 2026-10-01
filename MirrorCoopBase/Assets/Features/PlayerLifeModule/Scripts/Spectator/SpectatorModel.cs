using System;
using UnityEngine;

namespace Features.PlayerLifeModule.Scripts.Spectator {
    public sealed class SpectatorModel {
        public bool IsSpectating { get; private set; }
        public bool IsWaitingForTargets { get; private set; }
        public Transform Target { get; private set; }
        public int TargetIndex { get; private set; }
        public string Label { get; private set; } = string.Empty;

        public event Action OnChanged;

        public void Begin() {
            if (IsSpectating)
                return;

            IsSpectating = true;
            ResetTarget();
            OnChanged?.Invoke();
        }

        public void Focus(Transform target, int targetIndex, string label) {
            if (IsWaitingForTargets == false && Target == target && TargetIndex == targetIndex && Label == label)
                return;

            IsWaitingForTargets = false;
            Target = target;
            TargetIndex = targetIndex;
            Label = label;
            OnChanged?.Invoke();
        }

        public void WaitForTargets(string label) {
            if (IsWaitingForTargets && Label == label)
                return;

            IsWaitingForTargets = true;
            Target = null;
            TargetIndex = 0;
            Label = label;
            OnChanged?.Invoke();
        }

        public void End() {
            if (IsSpectating == false)
                return;

            IsSpectating = false;
            ResetTarget();
            OnChanged?.Invoke();
        }

        private void ResetTarget() {
            IsWaitingForTargets = false;
            Target = null;
            TargetIndex = 0;
            Label = string.Empty;
        }
    }
}
