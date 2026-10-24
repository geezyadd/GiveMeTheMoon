using Features.NetworkModelModule.Scripts;

namespace Features.ShipModule.Scripts {
    // Who may take or leave a seat. The occupant is the player's stable key, so a player who comes back gets the
    // same key, but the server frees the seat when the player leaves: a returning player is never seated by it.
    public sealed class ShipSeatRule {
        public bool CanSit(bool isSittable, PlayerKey occupant, PlayerKey rider) =>
            isSittable && rider.IsEmpty == false && occupant.IsEmpty;

        public bool IsHeldBy(PlayerKey occupant, PlayerKey rider) =>
            rider.IsEmpty == false && occupant.Equals(rider);
    }
}
