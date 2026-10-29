using Features.ParcelModule.Scripts.Configuration;
using Features.ParcelModule.Scripts.Rules;
using Features.ParcelModule.Scripts.Services;
using Features.ParcelModule.Scripts.Sonar;
using Features.ParcelModule.Scripts.Systems;
using Features.SonarModule.Contracts;
using Zenject;

namespace Features.ParcelModule.Scripts.Installers {
    public sealed class ParcelModuleInstaller : Installer<ParcelModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<ParcelConfigurationValidator>().AsSingle();
            Container.Bind<IParcelOfferRule>().To<ParcelOfferRule>().AsSingle();
            Container.Bind<IParcelSettleRule>().To<ParcelSettleRule>().AsSingle();
            Container.Bind<IParcelLossRule>().To<ParcelLossRule>().AsSingle();
            Container.Bind<IParcelWeaponIds>().To<ParcelWeaponIds>().AsSingle();
            Container.Bind<IParcelEntryService>().To<ParcelEntryService>().AsSingle();
            Container.Bind<IParcelAboardService>().To<ParcelAboardService>().AsSingle();
            Container.Bind<IParcelTheftService>().To<ParcelTheftService>().AsSingle();
            Container.Bind<IParcelCrateService>().To<ParcelCrateService>().AsSingle();
            Container.Bind<IParcelRecoveryService>().To<ParcelRecoveryService>().AsSingle();
            Container.Bind<IParcelSettleService>().To<ParcelSettleService>().AsSingle();
            Container.Bind<IParcelOfferService>().To<ParcelOfferService>().AsSingle();
            Container.Bind<IParcelStandService>().To<ParcelStandService>().AsSingle();
            Container.Bind<IParcelStandSpawnService>().To<ParcelStandSpawnService>().AsSingle();
            Container.Bind<ISonarContactSource>().To<ParcelSonarContactSource>().AsSingle();
            Container.BindInterfacesTo<ParcelWeaponTargetSystem>().AsSingle();
            Container.BindInterfacesTo<ParcelStationSystem>().AsSingle();
            Container.BindInterfacesTo<ParcelFlightSystem>().AsSingle();
            Container.BindInterfacesTo<ParcelPrefabSystem>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<Debug.ParcelDebugSystem>().AsSingle();
#endif
        }
    }
}
