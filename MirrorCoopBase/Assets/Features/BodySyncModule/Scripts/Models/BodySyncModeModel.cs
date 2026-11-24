using System;

namespace Features.BodySyncModule.Models {
    // How this peer shows the other players' bodies: by the old network body (off), or as simulated copies pulled
    // toward their owners' state packets (on). Local and unsynced, switched from the debug window, so two peers can
    // compare both ways at once. On by default.
    public sealed class BodySyncModeModel {
        public bool SimulatesCopies { get; private set; } = true;

        public event Action OnChanged;

        public void SetSimulatesCopies(bool simulatesCopies) {
            if (SimulatesCopies == simulatesCopies)
                return;

            SimulatesCopies = simulatesCopies;
            OnChanged?.Invoke();
        }
    }
}
