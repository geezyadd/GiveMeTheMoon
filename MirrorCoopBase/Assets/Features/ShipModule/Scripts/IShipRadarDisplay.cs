using UnityEngine;

namespace Features.ShipModule.Scripts {
    public interface IShipRadarDisplay {
        public void Attach(RectTransform slot);
        public void Detach();
    }
}
