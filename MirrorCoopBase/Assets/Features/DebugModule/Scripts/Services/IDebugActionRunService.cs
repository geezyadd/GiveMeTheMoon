using Features.DebugModule.Contracts;

namespace Features.DebugModule.Scripts.Services {
    public interface IDebugActionRunService {
        public DebugActionRunResult Run(DebugActionDefinition action, float value);
    }
}
