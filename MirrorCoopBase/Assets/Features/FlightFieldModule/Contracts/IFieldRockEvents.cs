using System;

namespace Features.FlightFieldModule.Contracts {
    // Server only: fires exactly once per broken rock.
    public interface IFieldRockEvents {
        public event Action<FieldRockDestroyed> RockDestroyed;
    }
}
