using Features.FieldAgentModule.Scripts.Cargo;
using UnityEngine;

namespace Features.BugModule.Scripts.Brain {
    // One bug on the server on top of its field agent (same id): its health, what it is doing and what for. Lives in
    // BugServerModel; written by the bug services.
    public sealed class BugAgent {
        public int Id { get; }
        // Its own side of the ship: the angle of its ring point round the ship, degrees.
        public float ApproachAngleDegrees { get; }
        public float Health { get; private set; }
        public BugStage Stage { get; private set; }
        // The ship-local point it lands on and gnaws (Land, Gnaw).
        public Vector3 GnawPoint { get; private set; }
        // The cargo it goes for or carries (SeekCargo, Carry).
        public IFieldAgentCargoTarget Cargo { get; private set; }
        public float SecondsToBite { get; private set; }
        // Where its field agent was at its last step: a cargo is let go here when the agent is already gone.
        public Vector3 LastWorldPosition { get; private set; }

        public bool IsDead => Health <= 0f;

        public BugAgent(int id, float approachAngleDegrees, float health) {
            Id = id;
            ApproachAngleDegrees = approachAngleDegrees;
            Health = health;
            Stage = BugStage.Approach;
        }

        public void RememberWorldPosition(Vector3 worldPosition) =>
            LastWorldPosition = worldPosition;

        public void TakeDamage(float damage) =>
            Health = Mathf.Max(0f, Health - damage);

        public void GoForCargo(IFieldAgentCargoTarget cargo) {
            Cargo = cargo;
            Stage = BugStage.SeekCargo;
        }

        public void CarryCargo() =>
            Stage = BugStage.Carry;

        public void GoLand(Vector3 gnawPoint) {
            Cargo = null;
            GnawPoint = gnawPoint;
            Stage = BugStage.Land;
        }

        public void StartGnawing(float secondsToBite) {
            Stage = BugStage.Gnaw;
            SecondsToBite = secondsToBite;
        }

        public void CountDownBite(float deltaTime) =>
            SecondsToBite -= deltaTime;

        public void WaitForNextBite(float secondsToBite) =>
            SecondsToBite += secondsToBite;

        public void Leave() =>
            Stage = BugStage.Leave;

        public void DropCargo() =>
            Cargo = null;
    }
}
