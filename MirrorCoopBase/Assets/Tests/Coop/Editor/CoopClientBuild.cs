using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Tests.Coop.Shared;
using UnityEditor;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Coop.Editor {
    // Builds the coop test client: Addressables content, then a Windows development player of the Build Settings
    // scenes. The coop test runs it before Play mode and skips the build when the content of Assets, Packages and
    // ProjectSettings is the same as after the last successful build (the build and Play mode re-save some files
    // without a change, so file times cannot tell).
    public sealed class CoopClientBuild : IPrebuildSetup {
        private const string MENU_PATH = "Tools/Coop Test/Build Client";
        private const string ANY_FILE = "*";
        // The Test Runner writes this scene into Assets right before the prebuild setup; it is not part of the client.
        private const string TEST_RUNNER_SCENE_PREFIX = "InitTestScene";
        // Play mode re-saves render textures (the minimap's) with runtime state the client does not need.
        private const string RENDER_TEXTURE_EXTENSION = ".renderTexture";
        // The Addressables content build writes these (git-ignored) outputs into Assets.
        private const string ADDRESSABLES_DATA_FOLDER = "AddressableAssetsData";
        private const string ADDRESSABLES_LINK_FILE = "link.xml";
        private const string ADDRESSABLES_STATE_EXTENSION = ".bin";
        private static readonly string[] _sourceFolders = { "Assets", "Packages", "ProjectSettings" };

        public void Setup() {
            if (IsUpToDate()) {
                Debug.Log($"[CoopClientBuild] {CoopClientBuildPaths.Executable} is up to date.");
                return;
            }

            Build();
        }

        [MenuItem(MENU_PATH)]
        public static void Build() {
            DateTime started = DateTime.Now;
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult content);
            if (string.IsNullOrEmpty(content.Error) == false)
                throw new BuildFailedException($"Addressables content build failed: {content.Error}");

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = CoopClientBuildPaths.Executable,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException($"The coop test client build {report.summary.result} with {report.summary.totalErrors} errors.");

            AssetDatabase.SaveAssets();
            File.WriteAllLines(CoopClientBuildPaths.BuildStamp, CreateSourceManifest());
            Debug.Log($"[CoopClientBuild] Built {CoopClientBuildPaths.Executable} in {(DateTime.Now - started).TotalSeconds:0} s.");
        }

        private static bool IsUpToDate() {
            if (File.Exists(CoopClientBuildPaths.Executable) == false || File.Exists(CoopClientBuildPaths.BuildStamp) == false)
                return false;

            string[] built = File.ReadAllLines(CoopClientBuildPaths.BuildStamp);
            List<string> current = CreateSourceManifest();
            string changed = current.Except(built).Concat(built.Except(current)).FirstOrDefault();
            if (changed == null)
                return true;

            Debug.Log($"[CoopClientBuild] Rebuilding: changed since the last build: {changed}");
            return false;
        }

        // One line per source file: its content hash and its path.
        private static List<string> CreateSourceManifest() {
            List<string> manifest = new List<string>();
            using MD5 md5 = MD5.Create();
            foreach (string file in EnumerateSourceFiles()) {
                using FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                manifest.Add($"{BitConverter.ToString(md5.ComputeHash(stream))} {file}");
            }

            return manifest;
        }

        private static IEnumerable<string> EnumerateSourceFiles() {
            List<string> files = new List<string>();
            foreach (string folder in _sourceFolders) {
                string path = Path.Combine(CoopClientBuildPaths.ProjectRoot, folder);
                files.AddRange(Directory.EnumerateFiles(path, ANY_FILE, SearchOption.AllDirectories).Where(IsClientSource));
            }

            files.Sort(StringComparer.Ordinal);
            return files;
        }

        private static bool IsClientSource(string file) =>
            Path.GetFileName(file).StartsWith(TEST_RUNNER_SCENE_PREFIX, StringComparison.Ordinal) == false
            && file.EndsWith(RENDER_TEXTURE_EXTENSION, StringComparison.Ordinal) == false
            && IsAddressablesBuildOutput(file) == false;

        private static bool IsAddressablesBuildOutput(string file) {
            if (file.Contains(ADDRESSABLES_DATA_FOLDER) == false)
                return false;

            string name = Path.GetFileName(file);
            return name.StartsWith(ADDRESSABLES_LINK_FILE, StringComparison.Ordinal) || name.EndsWith(ADDRESSABLES_STATE_EXTENSION, StringComparison.Ordinal);
        }
    }
}
