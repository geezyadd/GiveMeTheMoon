namespace Features.ShipDamageModule.Scripts.Damage {
    public interface IShipHullLeakRule {
        public ShipHullLeakStep Advance(float pendingLeak, int openBreaches, float deltaTime);
    }
}
