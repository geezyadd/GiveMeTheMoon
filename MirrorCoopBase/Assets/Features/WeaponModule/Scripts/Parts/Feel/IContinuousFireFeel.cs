namespace Features.WeaponModule.Scripts.Parts.Feel {
    // A weapon feel that shows the fire while the trigger is held (barrels spinning, a fire shake), not only its shots.
    public interface IContinuousFireFeel {
        public void PresentFiring(bool isFiring, float readiness);
    }
}
