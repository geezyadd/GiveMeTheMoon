#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.ShipModule.Scripts {
    internal interface IShipRidersDebug {
        public void DebugBindRider(ShipRider rider);
    }
}
#endif
