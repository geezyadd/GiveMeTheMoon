using Features.GrabModule.Scripts;
using UnityEngine;

namespace Features.ShipModule.Scripts {
    internal interface IShipDeckCargo {
        public int AttachedCount { get; }
        public void ClientAttach(uint netId, Vector3 localPosition, Quaternion localRotation);
        public void ClientDetach(uint netId);
        public void DetachAll(bool tellClients);
        public void DebugAttach(Grabbable grabbable);
        public bool TryMeasure(out Vector3 localPosition, out float drift);
    }
}
