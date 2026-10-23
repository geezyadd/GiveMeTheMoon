using Features.WeaponModule.Scripts.Hits;
using Features.WeaponModule.Scripts.Validation;
using Zenject;

namespace Features.WeaponModule.Scripts {
    public sealed class WeaponModuleInstaller : Installer<WeaponModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<WeaponCatalogValidator>().AsSingle();
            Container.BindInterfacesTo<WeaponHitRuleConfigurationValidator>().AsSingle();
            Container.Bind<IWeaponHitRule>().To<WeaponHitRule>().AsSingle();
        }
    }
}
