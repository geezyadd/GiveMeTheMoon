#if UNITY_INCLUDE_TESTS
using System;
using Features.FieldAgentModule.Scripts.Cargo;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.Bug {
    // A stand-in cargo for the bug tests, apart from the real parcel: a small crate on the deck, a child of the
    // ship until a bug takes it, then it hangs under its carrier; it records being taken and let go.
    public sealed class BugTestCargo : IFieldAgentCargoTarget, IDisposable {
        private const float CRATE_METERS = 0.6f;
        private const float HANG_BELOW_METERS = 0.6f;

        private readonly GameObject _crate;

        public bool IsTaken { get; private set; }
        public bool IsReleased { get; private set; }
        public Vector3 ShipLocalPosition { get; }
        public Vector3 CrateWorldPosition => _crate.transform.position;

        public BugTestCargo(Transform ship, Vector3 shipLocalPosition) {
            ShipLocalPosition = shipLocalPosition;
            _crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _crate.name = "BugTestCargo";
            Object.Destroy(_crate.GetComponent<Collider>());
            _crate.transform.SetParent(ship, false);
            _crate.transform.localPosition = shipLocalPosition + Vector3.up * (CRATE_METERS / 2f);
            _crate.transform.localScale = Vector3.one * CRATE_METERS;
        }

        public void Dispose() {
            if (_crate != null)
                Object.Destroy(_crate);
        }

        public void ServerTake(int carrierAgentId) {
            IsTaken = true;
            _crate.transform.SetParent(null, true);
        }

        public void ServerRelease(Vector3 worldPosition) {
            IsReleased = true;
            _crate.transform.position = worldPosition;
        }

        public void FollowCarrier(Vector3 carrierWorldPosition) =>
            _crate.transform.position = carrierWorldPosition + Vector3.down * HANG_BELOW_METERS;
    }
}
#endif
