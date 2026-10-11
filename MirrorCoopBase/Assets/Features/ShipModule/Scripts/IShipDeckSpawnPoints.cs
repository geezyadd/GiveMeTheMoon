using UnityEngine;

namespace Features.ShipModule.Scripts {
    // Where a player who comes back into the run appears on the ship's deck.
    public interface IShipDeckSpawnPoints {
        public bool HasSpawnPoint { get; }

        // The next point in turn, so players revived one after another do not land on each other.
        public Pose TakeSpawnPose();
    }
}
