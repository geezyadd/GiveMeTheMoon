using Zenject;

namespace Features.CoroutineRunnerModule {
    public sealed class CoroutineRunnerModuleInstaller : Installer<CoroutineRunnerModuleInstaller> {
        public override void InstallBindings() {
            Container.Bind<ICoroutineRunner>().To<CoroutineRunner>().FromComponentInHierarchy().AsSingle();
        }
    }
}
