using Features.FlightFieldModule.Scripts.Requests;

namespace Features.StagePresetModule.Editor.Tests {
    public sealed class RecordingFieldProfileDebug : IFieldProfileDebug {
        public string UsedProfileId { get; private set; }
        public int ClearCount { get; private set; }

        public void DebugUseProfile(string profileId) =>
            UsedProfileId = profileId;

        public void DebugClearProfile() =>
            ClearCount++;
    }
}
