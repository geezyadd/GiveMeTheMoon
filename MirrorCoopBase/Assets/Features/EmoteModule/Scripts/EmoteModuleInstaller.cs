using Zenject;

namespace Features.EmoteModule {
    public sealed class EmoteModuleInstaller : Installer<EmoteModuleInstaller> {
        public override void InstallBindings() =>
            Container.BindInterfacesTo<EmoteWheelSystem>().AsSingle();
    }
}
