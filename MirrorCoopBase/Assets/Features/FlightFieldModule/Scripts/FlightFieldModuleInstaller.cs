using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Generation.Passage;
using Features.FlightFieldModule.Scripts.Generation.Placement;
using Features.FlightFieldModule.Scripts.Generation.Routes;
using Features.FlightFieldModule.Scripts.Generation.Steps;
using Features.FlightFieldModule.Scripts.Network;
using Features.FlightFieldModule.Scripts.Requests;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Server;
using Features.FlightFieldModule.Scripts.Structures;
using Features.MatrixModule.Scripts.MatrixFactory;
using Zenject;

namespace Features.FlightFieldModule.Scripts {
    public sealed class FlightFieldModuleInstaller : Installer<FlightFieldModuleInstaller> {
        public override void InstallBindings() {
            Container.BindInterfacesTo<FieldStructureCatalogValidator>().AsSingle();
            Container.BindInterfacesTo<FlightFieldConfigurationValidator>().AsSingle();
            Container.Bind<IMatrixFactory>().To<MatrixFactory>().AsSingle();
            Container.Bind<IFieldRandomFactory>().To<FieldRandomFactory>().AsSingle();
            Container.Bind<IFieldFootprintReader>().To<FieldFootprintReader>().AsSingle();
            Container.Bind<IFieldStructurePicker>().To<FieldStructurePicker>().AsSingle();
            Container.Bind<IFieldStructurePlacer>().To<FieldStructurePlacer>().AsSingle();
            Container.Bind<IFieldObstacleScatterer>().To<FieldObstacleScatterer>().AsSingle();
            Container.Bind<IFieldAreaReserver>().To<FieldAreaReserver>().AsSingle();
            Container.Bind<IFieldRouteShaper>().To<CurveFieldRouteShaper>().AsSingle();
            Container.Bind<IFieldCenterlinePlanner>().To<FieldCenterlinePlanner>().AsSingle();
            Container.Bind<IFieldCorridorMapper>().To<FieldCorridorMapper>().AsSingle();
            Container.Bind<IFieldPassageChecker>().To<FieldPassageChecker>().AsSingle();
            Container.Bind<IFieldCorridorClearer>().To<FieldCorridorClearer>().AsSingle();
            InstallGenerationSteps();
            Container.Bind<IFlightFieldGenerator>().To<FlightFieldGenerator>().AsSingle();
            Container.BindInterfacesTo<FlightFieldGenerationWarmup>().AsSingle();
            Container.Bind<IFieldRequestFactory>().To<FieldRequestFactory>().AsSingle();
            InstallLiveField();
        }

        // The field in the world: the server publishes it at launch, every peer builds it from the live field model.
        private void InstallLiveField() {
            Container.Bind<IFieldStructureSpawnRule>().To<FieldStructureSpawnRule>().AsSingle();
            Container.Bind<IFieldStructureLayout>().To<FieldStructureLayout>().AsSingle();
            Container.Bind<IFlightFieldPublisher>().To<FlightFieldPublisher>().AsSingle();
            Container.Bind<IFieldNetworkedStructures>().To<FieldNetworkedStructures>().AsSingle();
            Container.BindInterfacesTo<FlightFieldService>().AsSingle();
            Container.Bind<IFieldStructurePool>().To<FieldStructurePool>().AsSingle();
            Container.Bind<IFieldStructureSceneBuilder>().To<FieldStructureSceneBuilder>().AsSingle();
            Container.BindInterfacesTo<LiveFieldBuildSystem>().AsSingle();
        }

        // The only place that orders generation: the steps run in the order they are bound here. A new kind of field
        // content is a new step class and one line here.
        private void InstallGenerationSteps() {
            Container.Bind<IFieldGenerationStep>().To<FieldFrameStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldStationZoneStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldTakeoffCorridorStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldRouteStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldWallStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldObstacleFillStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldRouteObstacleStep>().AsSingle();
            Container.Bind<IFieldGenerationStep>().To<FieldPassageStep>().AsSingle();
        }
    }
}
