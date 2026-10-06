namespace Features.ShipModule.Scripts {
    public interface IShipRunService {
        public void Bind(ShipRunDirector director, ShipBase ship, ShipLandingPad startPad);
        public void Unbind(ShipRunDirector director);
        public bool ServerTryLaunch(ShipBase ship);
        public void ServerAbort(ShipRunAbortReason reason);
        public void ServerTick();
    }
}
