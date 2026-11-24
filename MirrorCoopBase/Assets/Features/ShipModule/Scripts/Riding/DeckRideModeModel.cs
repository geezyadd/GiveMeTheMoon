using System;

namespace Features.ShipModule.Scripts.Riding {
    // How this peer's own player stands on a flying ship's deck. On (the default): it stays a physical body, carried by
    // the ship as a carrier (CarrierRider) and synced in the ship's axes like on any moving platform. Off: the old ride,
    // bound kinematically to the deck by ShipRider. Seats always bind. Local and unsynced, switched from the debug window;
    // each peer decides for its own player, and the others follow whatever that player does.
    public sealed class DeckRideModeModel {
        public bool UsesCarrier { get; private set; } = true;

        public event Action OnChanged;

        public void SetUsesCarrier(bool usesCarrier) {
            if (UsesCarrier == usesCarrier)
                return;

            UsesCarrier = usesCarrier;
            OnChanged?.Invoke();
        }
    }
}
