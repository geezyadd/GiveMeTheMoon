namespace Features.ShipModule.Scripts.Damage {
    public interface IShipDamageRandom {
        // A roll in [0, 1).
        public float Roll();
    }
}
