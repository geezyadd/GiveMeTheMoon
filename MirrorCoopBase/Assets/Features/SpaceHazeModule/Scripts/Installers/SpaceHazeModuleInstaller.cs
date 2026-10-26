using Features.SpaceHazeModule.Scripts.Core;
using Features.SpaceHazeModule.Scripts.Rendering;
using Features.SpaceHazeModule.Scripts.Systems;
using Zenject;

namespace Features.SpaceHazeModule.Scripts.Installers {
    public sealed class SpaceHazeModuleInstaller : Installer<SpaceHazeModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<ISpaceHazeRule>().To<SpaceHazeRule>().AsSingle();
            Container.Bind<ISpaceHazeSkySource>().To<SpaceHazeSkySource>().AsSingle();
            Container.BindInterfacesTo<SpaceHazeRenderPass>().AsSingle();
            Container.BindInterfacesTo<SpaceHazeSystem>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<SpaceHazeDebugSystem>().AsSingle();
#endif
        }
    }
}
