using Features.CameraModule.Contracts;
using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using Features.CameraModule.Scripts.Shake;
using Features.GameCoreModule.Contracts;
using Zenject;

namespace Features.CameraModule.Scripts {
    public sealed class CameraModuleInstaller : Installer<CameraModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<CursorModel>().AsSingle();
            Container.Bind<CameraLookLockModel>().AsSingle();
            Container.BindInterfacesTo<GameCameraService>().AsSingle();
            Container.BindInterfacesTo<CameraLookDriver>().AsSingle();
            Container.BindInterfacesTo<CameraSwitchBinder>().AsSingle();
            Container.Bind<ILocalViewpointSource>().To<CameraViewpointSource>().AsSingle();
            Container.Bind<ICameraShakeService>().To<CameraShakeService>().AsSingle();
            Container.Bind<ICameraShakeSampler>().To<CameraShakeSampler>().AsSingle();
            Container.BindInterfacesTo<CameraShakeSystem>().AsSingle();
        }
    }
}
