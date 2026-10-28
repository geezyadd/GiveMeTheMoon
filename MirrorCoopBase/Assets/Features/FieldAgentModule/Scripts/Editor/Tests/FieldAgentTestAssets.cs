using Features.FieldAgentModule.Scripts.Configuration;
using Features.SpaceHazeModule.Scripts.Configurations;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Editor.Tests {
    // Fresh configuration objects with their default values for one test; the caller destroys them.
    public static class FieldAgentTestAssets {
        public static FieldAgentProfile CreateProfile() =>
            ScriptableObject.CreateInstance<FieldAgentProfile>();

        public static FieldAgentConfiguration CreateConfiguration() =>
            ScriptableObject.CreateInstance<FieldAgentConfiguration>();

        public static SpaceHazeConfiguration CreateHaze() =>
            ScriptableObject.CreateInstance<SpaceHazeConfiguration>();
    }
}
