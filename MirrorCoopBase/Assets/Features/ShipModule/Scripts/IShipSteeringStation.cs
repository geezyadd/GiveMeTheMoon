namespace Features.ShipModule.Scripts {
    // A seat station that steers the ship (the helm). The server's flight reads its steer; every peer asks whether the
    // local player steers, to show the ship it steers without the interpolation delay.
    public interface IShipSteeringStation {
        public bool HasPilot { get; }
        public bool IsSteeredByLocalPlayer { get; }

        public float ReadSteer();
    }
}
