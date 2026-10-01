using Features.GameCoreModule.Scripts;
using Zenject;

namespace Features.TooltipModule.Scripts.Installers {
    public sealed class TooltipModuleInstaller : Installer<TooltipModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<IGameplaySession>().To<TooltipSession>().AsSingle();
        }
    }
}
