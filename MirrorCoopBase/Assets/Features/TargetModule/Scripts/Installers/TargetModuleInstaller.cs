using Zenject;

namespace Features.TargetModule.Installers {
    public sealed class TargetModuleInstaller : Installer<TargetModuleInstaller> {
        public override void InstallBindings() =>
            Container.Bind<TargetRegistry>().AsSingle();
    }
}
