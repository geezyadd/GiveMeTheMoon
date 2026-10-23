using Features.FlightFieldModule.Scripts.Configuration;

namespace Features.FlightFieldModule.Scripts.Requests {
    public interface IFieldProfileLookup {
        // The profile a synced field id names; a peer that does not know it has a broken field.
        public FieldDifficultyProfile GetRequired(string id);
        public bool TryFind(string id, out FieldDifficultyProfile profile);
    }
}
