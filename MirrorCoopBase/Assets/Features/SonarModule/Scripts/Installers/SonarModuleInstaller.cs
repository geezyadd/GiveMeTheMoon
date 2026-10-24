using Features.SonarModule.Contracts;
using Features.SonarModule.Data;
using Features.SonarModule.Scripts.Core;
using Features.SonarModule.Scripts.Sources;
using Features.SonarModule.Scripts.Systems;
using Zenject;

namespace Features.SonarModule.Scripts.Installers {
    public sealed class SonarModuleInstaller : Installer<SonarModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<SonarConfigurationValidator>().AsSingle();
            Container.Bind<SonarConsoleRequests>().AsSingle();
            InstallContacts();
            Container.Bind<ISonarRangeService>().To<SonarRangeService>().AsSingle();
            Container.Bind<ISonarMarkRule>().To<SonarMarkRule>().AsSingle();
            Container.Bind<ISonarConsoleRule>().To<SonarConsoleRule>().AsSingle();
            Container.Bind<ISonarPingRule>().To<SonarPingRule>().AsSingle();
            Container.Bind<ISonarScopeLayout>().To<SonarScopeLayout>().AsSingle();
            Container.Bind<ISonarMarkerProjection>().To<SonarMarkerProjection>().AsSingle();
            Container.BindInterfacesTo<SonarNavigatorSystem>().AsSingle();
            Container.BindInterfacesTo<SonarMarkSystem>().AsSingle();
            Container.BindInterfacesTo<SonarWindowSystem>().AsSingle();
            Container.BindInterfacesTo<SonarConsoleReachSystem>().AsSingle();
            Container.BindInterfacesTo<SonarScopeSystem>().AsSingle();
            Container.BindInterfacesTo<SonarMarkerSystem>().AsSingle();
        }

        // A new family of contacts (targets, enemies) is one more ISonarContactSource binding here.
        private void InstallContacts() {
            Container.Bind<IRockSonarContactIds>().To<RockSonarContactIds>().AsSingle();
            Container.Bind<ISonarContactSource>().To<RockSonarContactSource>().AsSingle();
            Container.Bind<ISonarContactSource>().To<StationSonarContactSource>().AsSingle();
            Container.Bind<ISonarContactSource>().To<TargetSonarContactSource>().AsSingle();
            Container.Bind<ISonarCondition>().To<RadarSonarCondition>().AsSingle();
            Container.Bind<ISonarContactService>().To<SonarContactService>().AsSingle();
        }
    }
}
