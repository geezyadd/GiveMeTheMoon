using System;

namespace Features.FlightFieldModule.Contracts {
    // Server only: fires exactly once per popped target balloon. What a pop gives (money now) is a subscriber's job.
    public interface IFieldTargetEvents {
        public event Action<FieldTargetPopped> TargetPopped;
    }
}
