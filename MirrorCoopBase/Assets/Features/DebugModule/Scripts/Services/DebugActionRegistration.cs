using System;
using Features.DebugModule.Contracts;
using Features.DebugModule.Scripts.Models;

namespace Features.DebugModule.Scripts.Services {
    public sealed class DebugActionRegistration : IDisposable {
        private readonly DebugActionsModel _debugActionsModel;
        private readonly DebugActionDefinition _action;

        public DebugActionRegistration(DebugActionsModel debugActionsModel, DebugActionDefinition action) {
            _debugActionsModel = debugActionsModel;
            _action = action;
        }

        public void Dispose() =>
            _debugActionsModel.Remove(_action);
    }
}
