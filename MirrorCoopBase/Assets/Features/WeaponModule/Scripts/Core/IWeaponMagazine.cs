namespace Features.WeaponModule.Scripts.Core {
    // A weapon's capability to run out of rounds and reload. The server keeps the true count; the holding peer spends
    // its own rounds at once so a shot never waits for the server, and takes the server's count whenever it arrives.
    public interface IWeaponMagazine {
        public int Capacity { get; }
        public int Rounds { get; }
        public bool IsReloading { get; }

        // 0 when a reload has just started, 1 when it is done.
        public float ReloadProgress { get; }

        // On this peer: a round is in and no reload is running.
        public bool HasRound { get; }

        public void SpendRound();

        public bool ServerTrySpendRound();

        public void ServerBeginReload();
    }
}
