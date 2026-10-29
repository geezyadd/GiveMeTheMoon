using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Behaviours {
    // Something an agent carries (the parcel): it follows the carrier's world position every tick.
    public interface IFieldAgentCargo {
        public void FollowCarrier(Vector3 carrierWorldPosition);
    }
}
