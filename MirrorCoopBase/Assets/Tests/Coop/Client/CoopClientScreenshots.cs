#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.IO;
using UnityEngine;

namespace Tests.Coop.Client {
    // What the client's own screen shows at a key moment, saved as client-<name>.png into the exchange folder; the host
    // test keeps them after the run.
    public sealed class CoopClientScreenshots {
        public const string FILE_PREFIX = "client-";
        private const string FILE_EXTENSION = ".png";

        private readonly string _directory;

        public CoopClientScreenshots(string directory) =>
            _directory = directory;

        // The capture is written at the end of the frame: the next frame starts after it.
        public IEnumerator CaptureCoroutine(string name) {
            string path = Path.Combine(_directory, FILE_PREFIX + name + FILE_EXTENSION);
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[CoopClient] screenshot {path}");
            yield return null;
            yield return null;
        }
    }
}
#endif
