using Features.BugModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Behaviours;
using Features.FieldAgentModule.Scripts.Cargo;
using UnityEngine;

namespace Features.BugModule.Scripts.Behaviours {
    // Builds the bug's stage behaviours from the field agent behaviours (seek, carry, attach, flee) with the bug's
    // numbers, each wrapped in the darting flight.
    public sealed class BugBehaviourFactory : IBugBehaviourFactory {
        private const float FULL_SPEED = 1f;

        private readonly BugConfiguration _bugConfiguration;

        public BugBehaviourFactory(BugConfiguration bugConfiguration) =>
            _bugConfiguration = bugConfiguration;

        public IFieldAgentBehaviour CreateApproach(float approachAngleDegrees) {
            Vector3 ringPoint = Quaternion.AngleAxis(approachAngleDegrees, Vector3.up) * Vector3.forward * _bugConfiguration.ApproachRingMeters
                + Vector3.up * _bugConfiguration.ApproachHeightMeters;
            return Dart(new SeekBehaviour(ringPoint, true, _bugConfiguration.ApproachArriveMeters, FULL_SPEED));
        }

        public IFieldAgentBehaviour CreateSeekCargo(IFieldAgentCargoTarget cargo) =>
            Dart(new SeekBehaviour(cargo.ShipLocalPosition, true, _bugConfiguration.CargoGrabMeters, FULL_SPEED));

        public IFieldAgentBehaviour CreateCarry(IFieldAgentCargoTarget cargo, Vector3 worldDestination) =>
            Dart(new CarryBehaviour(cargo, worldDestination, _bugConfiguration.CarryArriveMeters, _bugConfiguration.CarrySpeedShare));

        public IFieldAgentBehaviour CreateLand(Vector3 gnawPoint) =>
            Dart(new AttachBehaviour(gnawPoint, _bugConfiguration.GnawArriveMeters));

        public IFieldAgentBehaviour CreateLeave() =>
            Dart(new FleeBehaviour(_bugConfiguration.FleeMeters, FULL_SPEED));

        private IFieldAgentBehaviour Dart(IFieldAgentBehaviour inner) =>
            new BugDartBehaviour(inner, _bugConfiguration);
    }
}
