using Features.CameraModule.Scripts.Models;
using Features.CameraModule.Scripts.Services;
using Zenject;

namespace Features.CameraModule.Scripts {
    public sealed class CameraModuleInstaller : Installer<CameraModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<CursorModel>().AsSingle();
            Container.BindInterfacesTo<GameCameraService>().AsSingle();
            Container.BindInterfacesTo<CameraLookDriver>().AsSingle();
            Container.BindInterfacesTo<CameraSwitchBinder>().AsSingle();
        }
    }
}
