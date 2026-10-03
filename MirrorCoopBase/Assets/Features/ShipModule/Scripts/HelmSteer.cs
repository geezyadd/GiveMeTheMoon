namespace Features.ShipModule.Scripts {
    // The last steer a remote helm pilot sent. It belongs to that pilot: once the helm is empty or someone else sits
    // there, it reads zero, so a ship never keeps turning on a pilot who has left.
    public sealed class HelmSteer {
        private uint _pilotNetId;
        private float _value;

        public void Set(uint pilotNetId, float value) {
            _pilotNetId = pilotNetId;
            _value = value;
        }

        public void Clear() {
            _pilotNetId = 0;
            _value = 0f;
        }

        public float Read(uint helmOccupantNetId) =>
            helmOccupantNetId != 0 && helmOccupantNetId == _pilotNetId ? _value : 0f;
    }
}
