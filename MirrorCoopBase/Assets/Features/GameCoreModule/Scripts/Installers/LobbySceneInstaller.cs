using Features.ConstellationModule.Scripts.Installers;
using Zenject;

namespace Features.GameCoreModule.Scripts.Installers {
    public sealed class LobbySceneInstaller : MonoInstaller<LobbySceneInstaller> {
        public override void InstallBindings() =>
            ConstellationModuleInstaller.Install(Container);
    }
}
