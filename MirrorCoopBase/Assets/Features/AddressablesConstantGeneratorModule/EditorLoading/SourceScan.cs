using System;
using System.IO;
using System.Linq;

namespace Features.AddressablesConstantGeneratorModule.EditorLoading {
    // Source scans of the "no code checks a concrete entry" guard tests: the C# files under a folder, outside an
    // excluded folder, whose text matches.
    public static class SourceScan {
        private const string SOURCE_PATTERN = "*.cs";

        public static string[] FindFilesMatching(string folder, string excludedFolder, Func<string, bool> isMatch) =>
            Directory.GetFiles(folder, SOURCE_PATTERN, SearchOption.AllDirectories)
                .Select(ToAssetPath)
                .Where(path => path.Contains(excludedFolder) == false)
                .Where(path => isMatch(File.ReadAllText(path)))
                .ToArray();

        private static string ToAssetPath(string path) =>
            path.Replace('\\', '/');
    }
}
