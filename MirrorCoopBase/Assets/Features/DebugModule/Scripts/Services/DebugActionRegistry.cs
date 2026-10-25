using System;
using Features.DebugModule.Contracts;
using Features.DebugModule.Scripts.Models;

namespace Features.DebugModule.Scripts.Services {
    public sealed class DebugActionRegistry : IDebugActionRegistry {
        private readonly DebugActionsModel _debugActionsModel;

        public DebugActionRegistry(DebugActionsModel debugActionsModel) =>
            _debugActionsModel = debugActionsModel;

        public IDisposable Register(DebugActionDefinition action) {
            _debugActionsModel.Add(action);
            return new DebugActionRegistration(_debugActionsModel, action);
        }
    }
}
