using Features.ShipModule.Scripts.Generated;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    public interface IShipRoute {
        public Vector3 LaunchForward { get; }
        public Vector3 DestinationPoint { get; }
        public Vector3 DestinationForward { get; }
        public ShipFlightMode SelectedFlightMode { get; }
        public bool IsTravel { get; }
        public bool HasArrived { get; }
        public Vector3 PlanLaunch(Vector3 from, Vector3 shipForward);
        public void BeginRoute(float cruiseSeconds);
        public void BeginCruise();
        public void RefreshPreview();
        public void ApplyFrame(bool tickWork);
        public Vector3 PadForward(Vector3 face);
        public void SetDestinationPoint(Vector3 point);
        public void ShiftDestination(Vector3 delta);
        public void EndRoute();
        public void Reset();
        public ShipRunState WithTransit(ShipRunState state);
    }
}
