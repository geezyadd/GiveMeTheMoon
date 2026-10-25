using Features.DebugModule.Contracts;
using Features.DebugModule.Scripts.Models;
using Zenject;

namespace Features.DebugModule.Scripts.Systems {
    // While the panel is open every action's availability is asked each frame, so a button greys out the moment the
    // ship lands or the player dies; the model tells the window only about changes.
    public sealed class DebugActionAvailabilitySystem : ITickable {
        private readonly DebugActionsModel _debugActionsModel;
        private readonly DebugPanelModel _debugPanelModel;

        public DebugActionAvailabilitySystem(DebugActionsModel debugActionsModel, DebugPanelModel debugPanelModel) {
            _debugActionsModel = debugActionsModel;
            _debugPanelModel = debugPanelModel;
        }

        public void Tick() {
            if (_debugPanelModel.IsOpen == false)
                return;

            for (int i = 0; i < _debugActionsModel.Actions.Count; i++) {
                DebugActionDefinition action = _debugActionsModel.Actions[i];
                _debugActionsModel.SetUnavailableReason(action, action.ReadUnavailableReason());
            }
        }
    }
}
