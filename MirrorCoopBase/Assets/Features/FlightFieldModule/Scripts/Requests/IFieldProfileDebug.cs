#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.FlightFieldModule.Scripts.Requests {
    // Editor / development only: every field from the next launch on gets the given profile, whatever the loop.
    public interface IFieldProfileDebug {
        public void DebugUseProfile(string profileId);
        public void DebugClearProfile();
    }
}
#endif
