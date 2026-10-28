using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Deck {
    // The changes between flight and the deck: the smooth landing onto a deck point, the take-off back up, and being
    // knocked off at once.
    public interface IFieldAgentTransitionService {
        public void BeginLanding(ref FieldAgentState state, in FieldAgentShipFrame ship, Vector3 deckPoint);
        public void StepLanding(ref FieldAgentState state, FieldAgentProfile profile, float deltaTime);
        public void BeginTakeOff(ref FieldAgentState state, FieldAgentProfile profile);
        public void StepTakeOff(ref FieldAgentState state, FieldAgentProfile profile, in FieldAgentShipFrame ship, float deltaTime);
        public void KnockOff(ref FieldAgentState state, in FieldAgentShipFrame ship, Vector3 worldVelocity);
    }
}
