namespace Features.ShipModule.Scripts.Hull {
    public interface IShipHullDamageRule {
        public float CalculateDealtDamage(float amount, float armor);
    }
}
