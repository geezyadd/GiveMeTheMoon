using Features.FieldAgentModule.Scripts.Cargo;
using UnityEngine;

namespace Features.BugModule.Scripts.Brain {
    // What a bug that reached the ship goes for: a cargo, or a ship-local point to gnaw.
    public readonly struct BugTarget {
        public BugTargetKind Kind { get; }
        public IFieldAgentCargoTarget Cargo { get; }
        public Vector3 GnawPoint { get; }

        private BugTarget(BugTargetKind kind, IFieldAgentCargoTarget cargo, Vector3 gnawPoint) {
            Kind = kind;
            Cargo = cargo;
            GnawPoint = gnawPoint;
        }

        public static BugTarget CreateCargo(IFieldAgentCargoTarget cargo) =>
            new(BugTargetKind.Cargo, cargo, Vector3.zero);

        public static BugTarget CreateGnaw(Vector3 gnawPoint) =>
            new(BugTargetKind.Gnaw, null, gnawPoint);
    }
}
