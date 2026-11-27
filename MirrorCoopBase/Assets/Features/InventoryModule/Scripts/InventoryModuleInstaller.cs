using Zenject;

namespace Features.InventoryModule.Scripts {
    public sealed class InventoryModuleInstaller : Installer<InventoryModuleInstaller> {
        public override void InstallBindings() =>
            Container.Bind<IInventoryItemDisplaySource>().To<InventoryItemDisplaySource>().AsSingle();
    }
}
