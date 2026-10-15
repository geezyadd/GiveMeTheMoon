using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Requests;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Editor {
    // The flight field generation as the game binds it, for editor tools and tests.
    public sealed class FlightFieldEditorComposition {
        public FlightFieldConfiguration Configuration { get; }
        public FieldStructureCatalog Catalog { get; }
        public IFlightFieldGenerator Generator { get; }
        public IFieldRequestFactory RequestFactory { get; }
        public IFieldFootprintReader FootprintReader { get; }

        public FlightFieldEditorComposition(FlightFieldConfiguration configuration, FieldStructureCatalog catalog, IFlightFieldGenerator generator,
                                            IFieldRequestFactory requestFactory, IFieldFootprintReader footprintReader) {
            Configuration = configuration;
            Catalog = catalog;
            Generator = generator;
            RequestFactory = requestFactory;
            FootprintReader = footprintReader;
        }
    }
}
