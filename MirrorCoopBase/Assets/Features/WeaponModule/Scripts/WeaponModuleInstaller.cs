using Features.WeaponModule.Scripts.Hits;
using Features.WeaponModule.Scripts.Shooting;
using Features.WeaponModule.Scripts.Targets;
using Features.WeaponModule.Scripts.Validation;
using Zenject;

namespace Features.WeaponModule.Scripts {
    public sealed class WeaponModuleInstaller : Installer<WeaponModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<WeaponCatalogValidator>().AsSingle();
            Container.BindInterfacesTo<WeaponHitRuleConfigurationValidator>().AsSingle();
            Container.Bind<IWeaponHitRule>().To<WeaponHitRule>().AsSingle();
            Container.Bind<IWeaponHitService>().To<WeaponHitService>().AsSingle();
            Container.BindInterfacesTo<WeaponTargetRegistry>().AsSingle();
            Container.BindInterfacesTo<PhysicsShotDeliveryContext>().AsSingle();
            Container.Bind<IWeaponShooterFactory>().To<WeaponShooterFactory>().AsSingle();
        }
    }
}
