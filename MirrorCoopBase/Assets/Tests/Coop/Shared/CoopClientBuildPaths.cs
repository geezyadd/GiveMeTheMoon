#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace Tests.Coop.Shared {
    // Where the editor builds the coop test client and where the host test starts it from: outside Assets, git-ignored.
    public static class CoopClientBuildPaths {
        private const string BUILD_FOLDER = "Builds/CoopClient";
        private const string EXECUTABLE_NAME = "CoopClient.exe";
        // Written after a successful build: an incremental player build keeps the old time on the executable.
        private const string BUILD_STAMP_NAME = "build.stamp";
        private const string RUNS_FOLDER = "Builds/CoopRuns";

        public static string ProjectRoot =>
            Directory.GetParent(Application.dataPath).FullName;

        public static string Executable =>
            Path.Combine(ProjectRoot, BUILD_FOLDER, EXECUTABLE_NAME);

        public static string BuildStamp =>
            Path.Combine(ProjectRoot, BUILD_FOLDER, BUILD_STAMP_NAME);

        public static string RunsDirectory =>
            Path.Combine(ProjectRoot, RUNS_FOLDER);
    }
}
#endif
