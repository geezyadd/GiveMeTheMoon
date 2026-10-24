using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Structures;

namespace Features.FlightFieldModule.Scripts.Rocks {
    // Every peer: makes a built field rock a weapon target under its rock id.
    public interface IFieldRockTargetBinder {
        public void Bind(FieldRockVisual rock, FieldRockId id);
    }
}
