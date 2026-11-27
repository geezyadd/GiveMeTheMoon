using Features.EmoteModule.UI;
using Features.LobbyModule.Scripts;
using Features.LobbyModule.Scripts.Pause;
using Features.MenuModule.Scripts;
using Features.MvpModule;
using Features.SettingsModule.UI;
using Features.ShopModule.Scripts.UI;
using Features.SonarModule.Scripts.UI;
using Zenject;

namespace Features.GameCoreModule.Scripts.Installers {
    public sealed class WindowsModuleInstaller : Installer<WindowsModuleInstaller> {
        public override void InstallBindings() {
            ViewSystemInstaller.Install(Container);
            UIByContextInstaller.Install(Container);

            Container.BindInterfacesAndSelfTo<MenuWindow>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameHudWindow>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShopWindow>().AsSingle();
            Container.BindInterfacesAndSelfTo<SonarWindow>().AsSingle();
            Container.BindInterfacesAndSelfTo<EmoteWheelWindow>().AsSingle();
            Container.BindInterfacesAndSelfTo<SettingsWindow>().AsSingle();
            Container.BindInterfacesAndSelfTo<PauseMenuWindow>().AsSingle();
            Container.Bind<IPauseMenuService>().To<PauseMenuService>().AsSingle();
            Container.BindInterfacesTo<PauseMenuEscapeSystem>().AsSingle();
            Container.BindInterfacesTo<GameFlowWindowsSystem>().AsSingle();
        }
    }
}
