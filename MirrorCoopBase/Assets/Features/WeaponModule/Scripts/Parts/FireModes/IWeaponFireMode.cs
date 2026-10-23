namespace Features.WeaponModule.Scripts.Parts.FireModes {
    // When shots are born while the trigger is held or pressed. Readiness is 0..1: barrel spin of a minigun, charge
    // of a harpoon; the feel shows it.
    public interface IWeaponFireMode {
        public float Readiness { get; }
        public bool IsFiring { get; }

        // Moves the mode on by deltaTime and returns how many shots were born in that time.
        public int Advance(bool isTriggerHeld, float deltaTime);
    }
}
