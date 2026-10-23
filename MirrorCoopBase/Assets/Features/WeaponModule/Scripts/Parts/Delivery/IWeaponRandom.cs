namespace Features.WeaponModule.Scripts.Parts.Delivery {
    public interface IWeaponRandom {
        // A value in [0, 1).
        public float NextUnitValue();
    }
}
