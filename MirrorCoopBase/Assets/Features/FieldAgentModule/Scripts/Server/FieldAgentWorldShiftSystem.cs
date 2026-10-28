using Features.FieldAgentModule.Scripts.Agents;
using Features.GameCoreModule.Contracts;
using Mirror;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Server {
    // Server: a floating-origin shift moves the flying agents and the world points of their behaviours, as it moves
    // the field's rocks; ship-space agents ride the ship's own shift.
    public sealed class FieldAgentWorldShiftSystem : IWorldShiftFollower {
        private readonly FieldAgentServerModel _fieldAgentServerModel;

        public FieldAgentWorldShiftSystem(FieldAgentServerModel fieldAgentServerModel) =>
            _fieldAgentServerModel = fieldAgentServerModel;

        public void FollowWorldShift(Vector3 delta) {
            if (NetworkServer.active)
                _fieldAgentServerModel.ShiftWorld(delta);
        }
    }
}
