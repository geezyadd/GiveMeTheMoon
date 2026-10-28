using Features.BugModule.Scripts.Behaviours;
using Features.BugModule.Scripts.Brain;
using Features.BugModule.Scripts.Configuration;
using Features.BugModule.Scripts.Gnaw;
using Features.BugModule.Scripts.Health;
using Features.BugModule.Scripts.Network;
using Features.BugModule.Scripts.Sonar;
using Features.BugModule.Scripts.Spawn;
using Features.BugModule.Scripts.View;
using Features.ShipModule.Scripts;
using Features.SonarModule.Contracts;
using Zenject;

namespace Features.BugModule.Scripts {
    public sealed class BugModuleInstaller : Installer<BugModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<IBugKindService>().To<BugKindService>().AsSingle();
            Container.BindInterfacesTo<BugConfigurationValidator>().AsSingle();
            Container.Bind<IBugBehaviourFactory>().To<BugBehaviourFactory>().AsSingle();
            Container.Bind<IBugTargetRule>().To<BugTargetRule>().AsSingle();
            Container.Bind<IBugGnawService>().To<BugGnawService>().AsSingle();
            Container.Bind<IBugBrainService>().To<BugBrainService>().AsSingle();
            Container.Bind<IBugWeaponIds>().To<BugWeaponIds>().AsSingle();
            Container.Bind<IBugRemovalService>().To<BugRemovalService>().AsSingle();
            Container.Bind<IBugDamageService>().To<BugDamageService>().AsSingle();
            Container.Bind<BugWeaponTarget>().AsSingle();
            Container.Bind<IBugSpawnService>().To<BugSpawnService>().AsSingle();
            Container.Bind<IBugRandomSpawnRule>().To<BugRandomSpawnRule>().AsSingle();
            Container.BindInterfacesTo<ComponentRegistry<BugDeathSync>>().AsSingle();
            Container.BindInterfacesTo<BugDeathEventBus>().AsSingle();
            Container.Bind<IBugDeathBroadcast>().To<BugDeathBroadcastService>().AsSingle();
            Container.Bind<IBugDeathEffectPool>().To<BugDeathEffectPool>().AsSingle();
            Container.Bind<ISonarContactSource>().To<BugSonarContactSource>().AsSingle();
            Container.BindInterfacesTo<BugWeaponTargetSystem>().AsSingle();
            Container.BindInterfacesTo<BugBrainSystem>().AsSingle();
            Container.BindInterfacesTo<BugRandomSpawnSystem>().AsSingle();
            Container.BindInterfacesTo<BugDeathViewSystem>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<Debug.BugDebugSystem>().AsSingle();
#endif
        }
    }
}
