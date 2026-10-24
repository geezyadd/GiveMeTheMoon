namespace Features.WeaponModule.Scripts.Parts.Delivery {
    // Gets the outcome of a shot. A delivery may call it right away (hitscan) or frames later (a projectile, a tether).
    public interface IShotOutcomeReceiver {
        public void ReceiveOutcome(in WeaponShotOutcome outcome);
    }
}
