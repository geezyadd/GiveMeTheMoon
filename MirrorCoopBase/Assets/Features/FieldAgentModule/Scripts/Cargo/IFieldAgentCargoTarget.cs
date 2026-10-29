using Features.FieldAgentModule.Scripts.Behaviours;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Cargo {
    // Server: something on the ship's deck an agent can grab and carry away (the parcel). It registers in
    // IFieldAgentCargoTargetRegistry while it lies on board; a carrying agent drags it with IFieldAgentCargo.
    public interface IFieldAgentCargoTarget : IFieldAgentCargo {
        public bool IsTaken { get; }
        public Vector3 ShipLocalPosition { get; }
        // The agent of this id grabbed it: it leaves the deck and follows that carrier from now on.
        public void ServerTake(int carrierAgentId);
        // The carrier is gone (shot down or out of the field): the cargo is let go at that point.
        public void ServerRelease(Vector3 worldPosition);
    }
}
