namespace Features.FlightFieldModule.Scripts.Generation {
    // One stage of field generation. The steps run in the order of their bindings in FlightFieldModuleInstaller.
    public interface IFieldGenerationStep {
        public void Execute(FieldGenerationContext context);
    }
}
