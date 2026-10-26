namespace Features.ShipModule.Scripts.Hull {
    public interface IShipHullRepair {
        public void ServerRepairAfterLanding();
        // Gives back hull HP up to the max; a destroyed hull is not brought back.
        public void ServerHeal(float amount);
    }
}
