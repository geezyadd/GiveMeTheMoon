using Features.StagePresetModule.Scripts.Core;
using Features.StagePresetModule.Scripts.Systems;
using Zenject;

namespace Features.StagePresetModule.Scripts.Installers {
    public sealed class StagePresetModuleInstaller : Installer<StagePresetModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<StagePresetCatalogValidator>().AsSingle();
            Container.Bind<IStagePresetSelectionService>().To<StagePresetSelectionService>().AsSingle();
            Container.Bind<IStagePresetSocketResolver>().To<StagePresetSocketResolver>().AsSingle();
            Container.BindInterfacesTo<StagePresetApplier>().AsSingle();
        }
    }
}
