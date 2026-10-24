namespace Features.WeaponModule.Scripts.Hits {
    // How many hits one shooter may still report with one weapon, and when that was last counted. The server keeps
    // one per shooter and weapon; WeaponHitRule refills and spends it.
    public sealed class WeaponShotBudget {
        public bool IsStarted { get; private set; }
        public float AvailableHits { get; private set; }
        public double CountedAtSeconds { get; private set; }

        public void Recount(float availableHits, double countedAtSeconds) {
            IsStarted = true;
            AvailableHits = availableHits;
            CountedAtSeconds = countedAtSeconds;
        }
    }
}
