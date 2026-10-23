using Features.FlightFieldModule.Contracts;

namespace Features.FlightFieldModule.Scripts.Rocks {
    public interface IFieldRockClassifier {
        public FieldRockSize GetSize(float diameter, bool isAlwaysLarge);
    }
}
