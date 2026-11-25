namespace Features.FloatingControllerModule {
    // A body's ride on carriers (on the player). The player's own body picks its carrier from what it stands on; a
    // remote copy rides the carrier its owner's state packet names, so both agree on what the body moves with.
    public interface ICarrierRider {
        // The carrier the body moves with now, or null on the still world.
        ICarrier Carrier { get; }

        // On a peer that simulates a copy of someone else's body: ride as a copy (true) or not at all (false).
        void RideAsCopy(bool isCopy);

        // While riding as a copy: the carrier to ride (null: the still world).
        void SetCopyCarrier(ICarrier carrier);

        // The body was put somewhere else (a respawn, a map change): the carrier is forgotten without giving its speed
        // back, and the body boards again from where it now stands.
        void DropCarrier();

        // The body is caught in the air by a carrier (a drifting player taken back by the ship): from the next physics
        // step it rides that carrier at rest on it, as if it had jumped from it, while the carrier holds it.
        void CatchInAir(ICarrier carrier);
    }
}
