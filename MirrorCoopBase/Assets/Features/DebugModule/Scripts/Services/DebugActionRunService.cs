using Features.DebugModule.Contracts;

namespace Features.DebugModule.Scripts.Services {
    // The availability is asked again at the click: the window's view of it may be a frame old.
    public sealed class DebugActionRunService : IDebugActionRunService {
        public DebugActionRunResult Run(DebugActionDefinition action, float value) {
            if (string.IsNullOrEmpty(action.ReadUnavailableReason()) == false)
                return DebugActionRunResult.Unavailable;

            action.Run(value);
            return DebugActionRunResult.Ran;
        }
    }
}
