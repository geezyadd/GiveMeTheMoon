using System;

namespace Features.FlightFieldModule.Contracts {
    // Server only: fires exactly once per piece of ore that reached the ship. What it gives (money now) is a
    // subscriber's job.
    public interface IFieldOreEvents {
        public event Action<FieldOreCollected> OreCollected;
    }
}
