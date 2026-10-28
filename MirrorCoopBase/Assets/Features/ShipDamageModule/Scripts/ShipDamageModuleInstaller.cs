using Features.ShipDamageModule.Scripts.Damage;
using Features.ShipDamageModule.Scripts.Repair;
using Features.ShipModule.Scripts;
using Zenject;

namespace Features.ShipDamageModule.Scripts {
    public sealed class ShipDamageModuleInstaller : Installer<ShipDamageModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<ShipDamageConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<ShipModuleCondition>().AsSingle();
            Container.BindInterfacesTo<ShipBreachCondition>().AsSingle();
            Container.BindInterfacesTo<ComponentRegistry<ShipHullDamagePoint>>().AsSingle();
            Container.BindInterfacesTo<ShipDamageRule>().AsSingle();
            Container.BindInterfacesTo<ShipDamageRandom>().AsSingle();
            Container.BindInterfacesTo<ShipConditionService>().AsSingle();
            Container.BindInterfacesTo<ShipDamageCandidateSource>().AsSingle();
            Container.BindInterfacesTo<ShipImpactDamageService>().AsSingle();
            Container.BindInterfacesTo<ShipLandingRepair>().AsSingle();
            Container.BindInterfacesTo<ShipImpactDamageSystem>().AsSingle();
            Container.BindInterfacesTo<ShipHullLeakRule>().AsSingle();
            Container.BindInterfacesTo<ShipHullLeakSystem>().AsSingle();
            Container.BindInterfacesTo<ComponentRegistry<ShipToolRack>>().AsSingle();
            Container.BindInterfacesTo<ShipToolRackStockService>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<Debug.ShipDamageDebugSystem>().AsSingle();
#endif
        }
    }
}
