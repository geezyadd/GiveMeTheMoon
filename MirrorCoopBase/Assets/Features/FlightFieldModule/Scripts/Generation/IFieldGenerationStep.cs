using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Generation {
    // One stage of field generation. The steps run in the order of their bindings in FlightFieldModuleInstaller; a
    // step runs only for the layouts it serves.
    public interface IFieldGenerationStep {
        public bool IsUsedBy(FieldLayoutKind layout);
        public void Execute(FieldGenerationContext context);
    }
}
