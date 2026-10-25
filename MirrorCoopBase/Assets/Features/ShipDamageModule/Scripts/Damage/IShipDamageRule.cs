using System.Collections.Generic;
using UnityEngine;

namespace Features.ShipDamageModule.Scripts.Damage {
    public interface IShipDamageRule {
        public bool TryPickBrokenModule(IReadOnlyList<ShipDamageCandidate> modules, Vector3 point, float strength, float roll, out string socketId);
        public bool TryPickBreach(IReadOnlyList<ShipDamageCandidate> points, int openBreaches, Vector3 point, float strength, float roll, out string pointId);
    }
}
