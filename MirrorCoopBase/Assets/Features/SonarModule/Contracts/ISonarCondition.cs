namespace Features.SonarModule.Contracts {
    // Whether the ship's radar works now: a broken radar opens the console, but its scope shows noise, no contacts.
    public interface ISonarCondition {
        public bool IsWorking { get; }
    }
}
