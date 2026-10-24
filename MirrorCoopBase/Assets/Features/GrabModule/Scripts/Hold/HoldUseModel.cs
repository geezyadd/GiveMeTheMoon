using System;
using UnityEngine;

namespace Features.GrabModule.Scripts.Hold {
    // The local player's running hold as the HUD shows it: 0 = not holding, up to 1 = done.
    public sealed class HoldUseModel {
        public float Progress { get; private set; }
        public bool IsHolding => Progress > 0f;

        public event Action OnProgressChanged;

        public void SetProgress(float progress) {
            if (Mathf.Approximately(Progress, progress))
                return;

            Progress = progress;
            OnProgressChanged?.Invoke();
        }
    }
}
