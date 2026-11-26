using Zenject;

namespace Features.PlayerReplicaModule.Installers {
    public sealed class PlayerReplicaModuleInstaller : Installer<PlayerReplicaModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<BodyStateChannelRegistry>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<Diagnostics.ReplicaStatsDebugSystem>().AsSingle();
#endif
        }
    }
}
