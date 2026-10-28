using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Ore {
    // How many pieces of ore a broken rock drops, by its size (small, big, giant), from two rolls of 0..1: one for the
    // chance, one for the count.
    public interface IFieldOreDropRule {
        public int GetCount(in FieldRockDescription rock, float chanceRoll, float countRoll);
    }
}
