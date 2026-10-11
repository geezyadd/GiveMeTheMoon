using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Systems;
using Zenject;

namespace Features.ShopModule.Scripts.Installers {
    public sealed class ShopModuleInstaller : Installer<ShopModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<ShopModel>().AsSingle();
            Container.Bind<ShopPurchaseRequestEventClass>().AsSingle();
            Container.Bind<ShopReviveRequestEventClass>().AsSingle();
            Container.Bind<IShopAccessRule>().To<ShopAccessRule>().AsSingle();
            Container.Bind<IShopAccessService>().To<ShopAccessService>().AsSingle();
            Container.BindInterfacesTo<ShopKioskConfigurationValidator>().AsSingle();
            Container.Bind<IShopPurchaseService>().To<ShopPurchaseService>().AsSingle();
            Container.Bind<IShopItemStatsService>().To<ShopItemStatsService>().AsSingle();
            Container.Bind<IShopItemSpawnService>().To<ShopItemSpawnService>().AsSingle();
            Container.Bind<IShopPurchaseSystem>().To<ShopPurchaseSystem>().AsSingle();
            Container.Bind<IReviveRule>().To<ReviveRule>().AsSingle();
            Container.BindInterfacesTo<ReviveConfigurationValidator>().AsSingle();
            Container.Bind<IDeadCrewService>().To<DeadCrewService>().AsSingle();
            Container.Bind<IReviveShopSystem>().To<ReviveShopSystem>().AsSingle();
            Container.BindInterfacesTo<ShopWindowSystem>().AsSingle();
            Container.BindInterfacesTo<ShopKioskRangeSystem>().AsSingle();
            Container.BindInterfacesTo<DeadCrewSystem>().AsSingle();
        }
    }
}
