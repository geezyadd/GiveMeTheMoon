using Features.LobbyModule.Scripts;
using Features.MenuModule.Scripts;
using Features.MvpModule;
using Features.ShopModule.Scripts.UI;
using Zenject;

namespace Features.GameCoreModule.Scripts.Installers {
    public sealed class WindowsModuleInstaller : Installer<WindowsModuleInstaller> {
        public override void InstallBindings() {
            ViewSystemInstaller.Install(Container);
            UIByContextInstaller.Install(Container);

            Container.BindInterfacesAndSelfTo<MenuWindow>().AsSingle();
            Container.BindInterfacesAndSelfTo<GameHudWindow>().AsSingle();
            Container.BindInterfacesAndSelfTo<ShopWindow>().AsSingle();
            Container.BindInterfacesTo<GameFlowWindowsSystem>().AsSingle();
        }
    }
}
