using Features.DebugModule.Contracts;
using Features.DebugModule.Scripts.Models;
using Features.DebugModule.Scripts.Services;
using Features.DebugModule.Scripts.Systems;
using Features.DebugModule.Scripts.UI;
using Zenject;

namespace Features.DebugModule.Scripts.Installers {
    // Editor / development builds only (the assembly's define constraint): a release build has no registry, so no
    // module registers a debug action there.
    public sealed class DebugModuleInstaller : Installer<DebugModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<DebugActionsModel>().AsSingle();
            Container.Bind<DebugPanelModel>().AsSingle();
            Container.Bind<IDebugActionRegistry>().To<DebugActionRegistry>().AsSingle();
            Container.Bind<IDebugActionRunService>().To<DebugActionRunService>().AsSingle();
            Container.BindInterfacesTo<DebugActionAvailabilitySystem>().AsSingle();
            Container.BindInterfacesTo<DebugWindowSystem>().AsSingle();
            Container.BindInterfacesAndSelfTo<DebugWindow>().AsSingle();
        }
    }
}
