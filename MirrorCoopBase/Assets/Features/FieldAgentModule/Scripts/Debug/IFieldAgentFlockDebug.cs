#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Features.FieldAgentModule.Scripts.Debug {
    // Debug only, on the host: the "Spawn test agents (N)" flock. Public for the loop tests; the flock's state is in
    // FieldAgentFlockDebugModel.
    public interface IFieldAgentFlockDebug {
        public int DebugSpawnTestFlock(int count);
        public void DebugRemoveAll();
    }
}
#endif
