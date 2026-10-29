using Features.CutsceneModule.Contracts;
using Features.CutsceneModule.Scripts.Network.Generated;
using Features.ShipModule.Scripts.Contracts;
using Mirror;
using Zenject;

namespace Features.CutsceneModule.Scripts.Network {
    // The Cutscene bridge as a component on the ship's network identity, and the skip request any player sends: one
    // press skips the cutscene for everyone.
    public sealed class CutsceneSync : CutsceneBridge {
        private IComponentRegistry<CutsceneSync> _cutsceneSyncRegistry;
        private ICutsceneDirector _cutsceneDirector;

        [Inject]
        private void InjectDependencies(IComponentRegistry<CutsceneSync> cutsceneSyncRegistry, ICutsceneDirector cutsceneDirector) {
            _cutsceneSyncRegistry = cutsceneSyncRegistry;
            _cutsceneDirector = cutsceneDirector;
        }

        private void OnEnable() =>
            _cutsceneSyncRegistry.Add(this);

        private void OnDisable() =>
            _cutsceneSyncRegistry.Remove(this);

        public void RequestSkip() =>
            CmdSkip();

        [Command(requiresAuthority = false)]
        private void CmdSkip() =>
            _cutsceneDirector.ServerSkip();
    }
}
