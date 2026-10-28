using Features.BugModule.Scripts.Brain;
using Features.BugModule.Scripts.Network;
using Features.FieldAgentModule.Scripts.Server;
using UnityEngine;

namespace Features.BugModule.Scripts.Health {
    // Takes the bug out of the bug model and its field agent out of the field; a kill also goes to every peer.
    public sealed class BugRemovalService : IBugRemovalService {
        private readonly BugServerModel _bugServerModel;
        private readonly IFieldAgentControl _fieldAgentControl;
        private readonly IBugDeathBroadcast _bugDeathBroadcast;

        public BugRemovalService(BugServerModel bugServerModel, IFieldAgentControl fieldAgentControl, IBugDeathBroadcast bugDeathBroadcast) {
            _bugServerModel = bugServerModel;
            _fieldAgentControl = fieldAgentControl;
            _bugDeathBroadcast = bugDeathBroadcast;
        }

        public void ServerKill(int bugId) {
            _bugDeathBroadcast.ServerBroadcastDeath(bugId);
            ServerRemoveGone(bugId);
        }

        public void ServerRemoveGone(int bugId) {
            if (_bugServerModel.TryGet(bugId, out BugAgent bug) && IsCarrying(bug))
                ReleaseCargo(bug);

            _bugServerModel.Remove(bugId);
            _fieldAgentControl.Despawn(bugId);
        }

        private void ReleaseCargo(BugAgent bug) {
            _fieldAgentControl.TryGetWorldPosition(bug.Id, out Vector3 position);
            bug.Cargo.ServerRelease(position);
            bug.DropCargo();
        }

        private static bool IsCarrying(BugAgent bug) =>
            bug.Stage == BugStage.Carry && bug.Cargo != null;
    }
}
