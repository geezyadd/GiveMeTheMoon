namespace Features.ShipModule.Scripts {
    internal interface IShipFlightControl {
        public ShipFlightMode Mode { get; }
        public bool IsTakeoffComplete { get; }
        public bool HasLanded { get; }
        public void LockControls();
        public void SetDebugSteer(bool active, float steer);
        public void DebugFace(UnityEngine.Vector3 worldForward);
    }
}
