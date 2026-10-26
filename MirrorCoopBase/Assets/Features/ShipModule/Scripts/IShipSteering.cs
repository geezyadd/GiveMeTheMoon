namespace Features.ShipModule.Scripts {
    internal interface IShipSteering {
        public float ReadSteer();
        public bool IsSteeredByLocalPlayer();
    }
}
