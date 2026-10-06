using Features.NetworkModelModule.Scripts;

namespace Features.PlayerLifeModule.Scripts {
    public interface IPlayerLifeReviver {
        public void ServerReviveAll();

        // The player is dead, their body is spawned and the ship has a deck spot for them.
        public bool CanServerRevive(PlayerKey key);

        // Brings a dead player back on the ship's deck; only valid when CanServerRevive is true.
        public void ServerRevive(PlayerKey key);
    }
}
