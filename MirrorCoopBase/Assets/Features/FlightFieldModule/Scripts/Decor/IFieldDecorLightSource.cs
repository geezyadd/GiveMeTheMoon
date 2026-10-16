using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    public interface IFieldDecorLightSource {
        public bool TryGetLightTravel(out Vector3 direction);
    }
}
