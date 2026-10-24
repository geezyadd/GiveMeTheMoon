namespace Features.ShipModule.Scripts {
    // Who steers the ship: the first installed steering station (the helm) with a pilot. The ship knows no seat roles;
    // a module steers by having its slot view implement IShipSteeringStation.
    internal sealed class ShipSteering : IShipSteering {
        private readonly ShipSocket[] _sockets;

        internal ShipSteering(ShipSocket[] sockets) =>
            _sockets = sockets;

        public float ReadSteer() =>
            TryGetPiloted(out IShipSteeringStation station) ? station.ReadSteer() : 0f;

        public bool IsSteeredByLocalPlayer() =>
            TryGetPiloted(out IShipSteeringStation station) && station.IsSteeredByLocalPlayer;

        private bool TryGetPiloted(out IShipSteeringStation piloted) {
            piloted = null;
            for (int i = 0; i < _sockets.Length; i++) {
                ShipSocket socket = _sockets[i];
                if (socket == null || socket.Station is not IShipSteeringStation station || station.HasPilot == false)
                    continue;

                piloted = station;
                return true;
            }

            return false;
        }
    }
}
