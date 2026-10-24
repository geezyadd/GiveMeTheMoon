using Features.SonarModule.Contracts;

namespace Features.SonarModule.Scripts.Sources {
    // The radar cannot break yet: the module breakdown binds its own ISonarCondition in place of this one.
    public sealed class WorkingSonarCondition : ISonarCondition {
        public bool IsWorking => true;
    }
}
