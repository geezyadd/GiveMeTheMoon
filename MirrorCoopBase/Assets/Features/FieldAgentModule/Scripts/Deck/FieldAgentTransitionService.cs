using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Deck {
    // Landing and take-off run in ship space on an eased curve, so they look the same while the ship flies and turns;
    // only the end of a take-off (or a knock-off) brings the agent back into the world, with the ship's velocity.
    public sealed class FieldAgentTransitionService : IFieldAgentTransitionService {
        private const float MIN_FLAT_SQR = 0.0001f;

        public void BeginLanding(ref FieldAgentState state, in FieldAgentShipFrame ship, Vector3 deckPoint) {
            state.Mode = FieldAgentMode.Landing;
            state.TransitionFrom = ship.ToShipPoint(state.Position);
            state.TransitionTo = deckPoint;
            state.TransitionElapsed = 0f;
            state.Position = state.TransitionFrom;
            state.Rotation = ship.ToShipRotation(state.Rotation);
            state.Velocity = Vector3.zero;
        }

        public void StepLanding(ref FieldAgentState state, FieldAgentProfile profile, float deltaTime) {
            state.TransitionElapsed += deltaTime;
            float progress = Mathf.Clamp01(state.TransitionElapsed / profile.LandingSeconds);
            Vector3 previous = state.Position;
            state.Position = Vector3.Lerp(state.TransitionFrom, state.TransitionTo, Mathf.SmoothStep(0f, 1f, progress));
            state.Rotation = Quaternion.Slerp(state.Rotation, CalculateLevel(state.Rotation), progress);
            state.Velocity = deltaTime > 0f ? (state.Position - previous) / deltaTime : Vector3.zero;
            if (progress < 1f)
                return;

            state.Mode = FieldAgentMode.Deck;
            state.Position = state.TransitionTo;
            state.Velocity = Vector3.zero;
        }

        public void BeginTakeOff(ref FieldAgentState state, FieldAgentProfile profile) {
            state.Mode = FieldAgentMode.TakingOff;
            state.TransitionFrom = state.Position;
            state.TransitionTo = state.Position + Vector3.up * profile.TakeOffHeight;
            state.TransitionElapsed = 0f;
        }

        public void StepTakeOff(ref FieldAgentState state, FieldAgentProfile profile, in FieldAgentShipFrame ship, float deltaTime) {
            state.TransitionElapsed += deltaTime;
            float progress = Mathf.Clamp01(state.TransitionElapsed / profile.TakeOffSeconds);
            Vector3 previous = state.Position;
            state.Position = Vector3.Lerp(state.TransitionFrom, state.TransitionTo, Mathf.SmoothStep(0f, 1f, progress));
            state.Velocity = deltaTime > 0f ? (state.Position - previous) / deltaTime : Vector3.zero;
            if (progress < 1f)
                return;

            KnockOff(ref state, ship, ship.Velocity + ship.ToWorldDirection(Vector3.up) * profile.TakeOffSpeed);
        }

        public void KnockOff(ref FieldAgentState state, in FieldAgentShipFrame ship, Vector3 worldVelocity) {
            if (state.IsInShipSpace) {
                state.Position = ship.ToWorldPoint(state.Position);
                state.Rotation = ship.ToWorldRotation(state.Rotation);
            }

            state.Mode = FieldAgentMode.Flight;
            state.Velocity = worldVelocity;
        }

        private static Quaternion CalculateLevel(Quaternion rotation) {
            Vector3 forward = rotation * Vector3.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > MIN_FLAT_SQR ? Quaternion.LookRotation(forward, Vector3.up) : Quaternion.identity;
        }
    }
}
