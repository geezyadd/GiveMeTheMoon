using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Behaviours {
    // The distance from an agent to a goal point in the space the point is given in, whatever space the agent is in.
    public static class FieldAgentGoalDistance {
        public static float Calculate(in FieldAgentBehaviourInput input, Vector3 point, bool isShipSpace) {
            if (isShipSpace == false)
                return Vector3.Distance(input.WorldPosition, point);

            return input.Ship.HasShip ? Vector3.Distance(input.ShipLocalPosition, point) : float.MaxValue;
        }
    }
}
