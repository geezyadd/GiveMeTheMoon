using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Cargo;
using Features.ShipDamageModule.Scripts.Damage;
using UnityEngine;

namespace Features.BugModule.Scripts.Brain {
    public interface IBugTargetRule {
        // spots: the ship's damage candidates with ship-local positions; bugShipLocal: where the bug is in ship space.
        public BugTarget Choose(IReadOnlyList<IFieldAgentCargoTarget> cargoTargets, IReadOnlyList<ShipDamageCandidate> spots, Vector3 bugShipLocal);
    }
}
