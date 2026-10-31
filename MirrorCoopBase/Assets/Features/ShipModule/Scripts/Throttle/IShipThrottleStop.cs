namespace Features.ShipModule.Scripts.Throttle {
    // Server: brings the throttle to Stop and lets its holder go, so the cruise eases to a halt at the flight's own
    // acceleration (a cutscene stops the ship this way); the crew pushes the lever again to fly on.
    public interface IShipThrottleStop {
        public void ServerStop();
    }
}
