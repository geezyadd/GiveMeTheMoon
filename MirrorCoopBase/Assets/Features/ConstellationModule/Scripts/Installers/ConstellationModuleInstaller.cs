using Features.ConstellationModule.Scripts.Core;
using Features.ConstellationModule.Scripts.Systems;
using Zenject;

namespace Features.ConstellationModule.Scripts.Installers {
    public sealed class ConstellationModuleInstaller : Installer<ConstellationModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<IConstellationMeshBuilder>().To<ConstellationMeshBuilder>().AsSingle();
            Container.Bind<IConstellationRevealTimelineFactory>().To<ConstellationRevealTimelineFactory>().AsSingle();
            Container.Bind<IConstellationFadeRule>().To<ConstellationFadeRule>().AsSingle();
            Container.BindInterfacesTo<ConstellationRevealSystem>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<ConstellationDebugSystem>().AsSingle();
#endif
        }
    }
}
