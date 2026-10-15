using Features.CrewInviteModule.Scripts.Core;
using Features.CrewInviteModule.Scripts.UI;
using Zenject;

namespace Features.CrewInviteModule.Scripts.Installers {
    public sealed class CrewInviteModuleInstaller : Installer<CrewInviteModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<ICrewInviteRule>().To<CrewInviteRule>().AsSingle();
            Container.Bind<ICrewInviteStatusService>().To<CrewInviteStatusService>().AsSingle();
            Container.BindInterfacesTo<CrewInviteTooltipProvider>().AsSingle();
        }
    }
}
