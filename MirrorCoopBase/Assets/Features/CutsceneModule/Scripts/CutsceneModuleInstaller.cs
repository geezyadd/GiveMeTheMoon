using Features.CutsceneModule.Contracts;
using Features.CutsceneModule.Scripts.Client;
using Features.CutsceneModule.Scripts.Configuration;
using Features.CutsceneModule.Scripts.Core;
using Features.CutsceneModule.Scripts.Network;
using Features.CutsceneModule.Scripts.Server;
using Features.ShipModule.Scripts;
using Zenject;

namespace Features.CutsceneModule.Scripts {
    public sealed class CutsceneModuleInstaller : Installer<CutsceneModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<CutsceneConfigurationValidator>().AsSingle();
            Container.BindInterfacesTo<ComponentRegistry<CutsceneSync>>().AsSingle();
            Container.Bind<ICutscenePlaybackRule>().To<CutscenePlaybackRule>().AsSingle();
            Container.Bind<ICutsceneCameraPath>().To<CutsceneCameraPath>().AsSingle();
            Container.Bind<ICutsceneDirector>().To<CutscenePlaybackService>().AsSingle();
            Container.BindInterfacesTo<CutsceneCameraSystem>().AsSingle();
            Container.BindInterfacesTo<CutscenePlayerLockSystem>().AsSingle();
            Container.BindInterfacesTo<CutsceneHudSystem>().AsSingle();
            Container.BindInterfacesTo<CutsceneSkipSystem>().AsSingle();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Container.BindInterfacesTo<Debug.CutsceneDebugSystem>().AsSingle();
#endif
        }
    }
}
