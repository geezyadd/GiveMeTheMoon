using Features.CutsceneModule.Contracts;
using Features.PointOfInterestModule.Scripts.Client;
using Features.PointOfInterestModule.Scripts.Configuration;
using Features.PointOfInterestModule.Scripts.Content;
using Features.PointOfInterestModule.Scripts.Network;
using Features.PointOfInterestModule.Scripts.Server;
using Features.PointOfInterestModule.Scripts.Sonar;
using Features.PointOfInterestModule.Scripts.Weapons;
using Features.ShipModule.Scripts;
using Features.SonarModule.Contracts;
using Zenject;

namespace Features.PointOfInterestModule.Scripts {
    public sealed class PointOfInterestModuleInstaller : Installer<PointOfInterestModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<PointOfInterestConfigurationValidator>().AsSingle();
            Container.Bind<IPointOfInterestContentCatalog>().To<PointOfInterestContentCatalog>().AsSingle();
            Container.BindInterfacesTo<ComponentRegistry<PointOfInterestSync>>().AsSingle();
            Container.BindInterfacesTo<LivePointsOfInterest>().AsSingle();
            Container.Bind<IPointOfInterestZoneRule>().To<PointOfInterestZoneRule>().AsSingle();
            Container.Bind<IPointOfInterestPlacementService>().To<PointOfInterestPlacementService>().AsSingle();
            Container.Bind<IPointOfInterestTriggerService>().To<PointOfInterestTriggerService>().AsSingle();
            Container.Bind<IPointOfInterestOpenService>().To<PointOfInterestOpenService>().AsSingle();
            Container.Bind<IPointOfInterestDamageService>().To<PointOfInterestDamageService>().AsSingle();
            Container.BindInterfacesTo<PointOfInterestServerSystem>().AsSingle();
            Container.BindInterfacesTo<PointOfInterestWorldShiftSystem>().AsSingle();
            InstallContents();
            Container.Bind<IPointOfInterestWeaponIds>().To<PointOfInterestWeaponIds>().AsSingle();
            Container.Bind<PointOfInterestWeaponTarget>().AsSingle();
            Container.BindInterfacesTo<PointOfInterestWeaponTargetSystem>().AsSingle();
            Container.Bind<ISonarContactSource>().To<PointOfInterestSonarContactSource>().AsSingle();
            Container.BindInterfacesTo<PointOfInterestViewSystem>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<Debug.PointOfInterestDebugSystem>().AsSingle();
#endif
        }

        // One class per content kind; a new kind is one more IPointOfInterestContent here and its definition asset.
        private void InstallContents() {
            Container.Bind<IPointOfInterestContent>().To<BugNestContent>().AsSingle();
            Container.Bind<IBugNestCutsceneShots>().To<BugNestCutsceneShots>().AsSingle();
            Container.Bind<IBugNestFlockTargetIds>().To<BugNestFlockTargetIds>().AsSingle();
            Container.Bind<ICutsceneTargetSource>().To<BugNestFlockTargetSource>().AsSingle();
        }
    }
}
