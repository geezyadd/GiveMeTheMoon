#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Debug {
    // Debug only, on the host: the deck trip's agent, its two ship-local deck points, the stage it is at and how long
    // it has stood at the module. Written by FieldAgentDeckTripDebugSystem, read by the tests.
    public sealed class FieldAgentDeckTripDebugModel {
        public FieldAgentDeckTripStage Stage { get; private set; }
        public int AgentId { get; private set; }
        public Vector3 LandingPoint { get; private set; }
        public Vector3 ModulePoint { get; private set; }
        public float AtModuleSeconds { get; private set; }
        public bool IsIdle => Stage == FieldAgentDeckTripStage.None || Stage == FieldAgentDeckTripStage.Lost || Stage == FieldAgentDeckTripStage.Orbiting;

        public void Begin(int agentId, Vector3 landingPoint, Vector3 modulePoint) {
            AgentId = agentId;
            LandingPoint = landingPoint;
            ModulePoint = modulePoint;
            AtModuleSeconds = 0f;
            Stage = FieldAgentDeckTripStage.FlyingIn;
        }

        public void SetStage(FieldAgentDeckTripStage stage) {
            Stage = stage;
            AtModuleSeconds = 0f;
        }

        public void AddAtModuleSeconds(float seconds) =>
            AtModuleSeconds += seconds;
    }
}
#endif
