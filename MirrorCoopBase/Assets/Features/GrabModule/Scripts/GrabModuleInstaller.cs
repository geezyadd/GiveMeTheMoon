using Zenject;

namespace Features.GrabModule.Scripts {
    public sealed class GrabModuleInstaller : Installer<GrabModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesAndSelfTo<HeldItemRegistry>().AsSingle();
            Container.Bind<InteractionReach>().AsSingle();
            Container.BindInterfacesTo<HeldItemFollowSystem>().AsSingle();
        }
    }
}
