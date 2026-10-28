using Features.FieldAgentModule.Scripts.Cargo;
using UnityEngine;

namespace Features.BugModule.Scripts.Editor.Tests {
    // A cargo on board for the tests (the parcel comes later): it records being taken, dragged and let go.
    public sealed class FakeCargoTarget : IFieldAgentCargoTarget {
        public bool IsTaken { get; private set; }
        public Vector3 ShipLocalPosition { get; }
        public Vector3 LastCarrierPosition { get; private set; }
        public bool IsReleased { get; private set; }

        public FakeCargoTarget(Vector3 shipLocalPosition, bool isTaken = false) {
            ShipLocalPosition = shipLocalPosition;
            IsTaken = isTaken;
        }

        public void ServerTake() =>
            IsTaken = true;

        public void ServerRelease(Vector3 worldPosition) {
            IsReleased = true;
            LastCarrierPosition = worldPosition;
        }

        public void FollowCarrier(Vector3 carrierWorldPosition) =>
            LastCarrierPosition = carrierWorldPosition;
    }
}
