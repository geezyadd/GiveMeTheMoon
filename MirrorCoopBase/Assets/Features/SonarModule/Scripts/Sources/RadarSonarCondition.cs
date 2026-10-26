using System;
using Features.ShipModule.Scripts.Contracts;
using Features.SonarModule.Contracts;
using Features.SonarModule.Data;
using Zenject;

namespace Features.SonarModule.Scripts.Sources {
    // The sonar works while the module in the console's radar socket works; a broken radar (a module breakdown) shows
    // noise until a mechanic repairs it.
    public sealed class RadarSonarCondition : ISonarCondition, IInitializable, IDisposable {
        private readonly IShipModuleCondition _shipModuleCondition;
        private readonly SonarConsoleBindingModel _sonarConsoleBindingModel;
        private string _checkedSocketId;
        private bool _isCheckedSocketWorking;

        public bool IsWorking =>
            _sonarConsoleBindingModel.IsBound && IsSocketWorking(_sonarConsoleBindingModel.SocketId);

        public RadarSonarCondition(IShipModuleCondition shipModuleCondition, SonarConsoleBindingModel sonarConsoleBindingModel) {
            _shipModuleCondition = shipModuleCondition;
            _sonarConsoleBindingModel = sonarConsoleBindingModel;
        }

        public void Initialize() =>
            _shipModuleCondition.OnConditionChanged += OnConditionChanged;

        public void Dispose() =>
            _shipModuleCondition.OnConditionChanged -= OnConditionChanged;

        // The scope reads the condition every frame; the module condition is asked again only when it changed or the
        // console was bound to another socket.
        private bool IsSocketWorking(string socketId) {
            if (string.Equals(socketId, _checkedSocketId, StringComparison.Ordinal))
                return _isCheckedSocketWorking;

            _checkedSocketId = socketId;
            _isCheckedSocketWorking = _shipModuleCondition.IsWorking(socketId);
            return _isCheckedSocketWorking;
        }

        private void OnConditionChanged() =>
            _checkedSocketId = null;
    }
}
