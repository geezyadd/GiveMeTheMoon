using UnityEngine;

namespace Features.ShipModule.Scripts {
    public interface IShipRoute {
        Vector3 LaunchForward { get; }
        Vector3 DestinationPoint { get; }
        Vector3 DestinationForward { get; }
        ShipFlightMode SelectedFlightMode { get; }
        bool IsTravel { get; }
        bool HasArrived { get; }
        Vector3 PlanLaunch(Vector3 from, Vector3 shipForward);
        void BeginRoute(float cruiseSeconds);
        void BeginCruise();
        void RefreshPreview();
        void ApplyFrame(bool tickWork);
        Vector3 PadForward(Vector3 face);
        void SetDestinationPoint(Vector3 point);
        void ShiftDestination(Vector3 delta);
        void EndRoute();
        void Reset();
        void DebugUseFlightMode(ShipFlightMode mode);
        void DebugClearFlightMode();
    }
}
