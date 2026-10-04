using Features.GameCoreModule.Contracts;
using Zenject;

namespace Features.TooltipModule.Scripts.Installers {
    public sealed class TooltipModuleInstaller : Installer<TooltipModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<IGameplaySession>().To<TooltipSession>().AsSingle();
            Container.Bind<ITooltipContentService>().To<TooltipContentService>().AsSingle();
        }
    }
}
