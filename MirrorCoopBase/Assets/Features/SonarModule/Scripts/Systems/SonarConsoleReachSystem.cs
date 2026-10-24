using Features.PlayerLifeModule.Scripts;
using Features.SonarModule.Data;
using Features.SonarModule.Data.Generated;
using Features.SonarModule.Scripts.Core;
using Zenject;

namespace Features.SonarModule.Scripts.Systems {
    // Closes the local console once the player may no longer stand at it: they died, moved away from it, whatever
    // moved them, or the server gave the console up (landing, the radar is gone).
    public sealed class SonarConsoleReachSystem : ITickable {
        private readonly SonarScopeModel _sonarScopeModel;
        private readonly IReadOnlySonarConsoleModel _sonarConsoleModel;
        private readonly SonarConsoleBindingModel _sonarConsoleBindingModel;
        private readonly IPlayerBodyRegistry _playerBodyRegistry;
        private readonly IPlayerLifeQuery _playerLifeQuery;
        private readonly ISonarConsoleRule _sonarConsoleRule;

        public SonarConsoleReachSystem(
            SonarScopeModel sonarScopeModel,
            IReadOnlySonarConsoleModel sonarConsoleModel,
            SonarConsoleBindingModel sonarConsoleBindingModel,
            IPlayerBodyRegistry playerBodyRegistry,
            IPlayerLifeQuery playerLifeQuery,
            ISonarConsoleRule sonarConsoleRule) {
            _sonarScopeModel = sonarScopeModel;
            _sonarConsoleModel = sonarConsoleModel;
            _sonarConsoleBindingModel = sonarConsoleBindingModel;
            _playerBodyRegistry = playerBodyRegistry;
            _playerLifeQuery = playerLifeQuery;
            _sonarConsoleRule = sonarConsoleRule;
        }

        public void Tick() {
            if (_sonarScopeModel.IsOpen == false)
                return;

            if (_sonarConsoleModel.NavigatorKey == _sonarScopeModel.NavigatorKey)
                _sonarScopeModel.ConfirmSeat();

            if (CanStayAtConsole() == false)
                _sonarScopeModel.Close();
        }

        private bool CanStayAtConsole() {
            PlayerLifeBody body = _playerBodyRegistry.LocalBody;
            if (_sonarConsoleBindingModel.IsBound == false || body == null || _playerLifeQuery.LocalState != PlayerLifeState.Alive)
                return false;

            return IsSeatLost() == false && _sonarConsoleRule.IsWithinReach(body.transform.position, _sonarConsoleBindingModel.Console.position);
        }

        // The synced key may still be empty for a moment after the seat message on a remote client: an empty key ends
        // the visit only once this peer saw its own key in the model.
        private bool IsSeatLost() {
            string syncedKey = _sonarConsoleModel.NavigatorKey;
            if (syncedKey == _sonarScopeModel.NavigatorKey)
                return false;

            return string.IsNullOrEmpty(syncedKey) == false || _sonarScopeModel.IsSeatConfirmed;
        }
    }
}
