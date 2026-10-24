using Features.ShipModule.Scripts.Damage;
using Features.SonarModule.Contracts;
using Features.SonarModule.Data;

namespace Features.SonarModule.Scripts.Sources {
    // The sonar works while the module in the console's radar socket works; a broken radar (a module breakdown) shows
    // noise until a mechanic repairs it.
    public sealed class RadarSonarCondition : ISonarCondition {
        private readonly IShipModuleCondition _shipModuleCondition;
        private readonly SonarConsoleBindingModel _sonarConsoleBindingModel;

        public bool IsWorking =>
            _sonarConsoleBindingModel.IsBound && _shipModuleCondition.IsWorking(_sonarConsoleBindingModel.SocketId);

        public RadarSonarCondition(IShipModuleCondition shipModuleCondition, SonarConsoleBindingModel sonarConsoleBindingModel) {
            _shipModuleCondition = shipModuleCondition;
            _sonarConsoleBindingModel = sonarConsoleBindingModel;
        }
    }
}
