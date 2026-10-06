namespace Features.ShipModule.Scripts {
    public interface IShipRunService {
        void Bind(ShipRunDirector director, ShipBase ship, ShipLandingPad startPad);
        void Unbind(ShipRunDirector director);
        bool ServerTryLaunch(ShipBase ship);
        void ServerAbort(ShipRunAbortReason reason);
        void ServerTick();
    }
}
