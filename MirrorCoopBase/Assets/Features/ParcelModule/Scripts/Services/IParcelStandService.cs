using Features.ParcelModule.Data;
using UnityEngine;

namespace Features.ParcelModule.Scripts.Services {
    // Every peer: whether a delivery stand offers now (the ship stands at its station) and its offer in a slot.
    public interface IParcelStandService {
        public bool IsOffering(Vector3 standPosition);
        public bool TryGetOffer(int slot, out ParcelOffer offer);
    }
}
