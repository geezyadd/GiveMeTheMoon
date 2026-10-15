#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using Tests.Coop.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // Starts the coop test client when a development build is launched with -coopTestClient; otherwise does nothing.
    public static class CoopTestClientStartup {
        private const string OBJECT_NAME = "CoopTestClient";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartIfRequested() {
            if (CoopTestArguments.TryRead(Environment.GetCommandLineArgs(), out CoopTestArguments arguments) == false)
                return;

            GameObject host = new GameObject(OBJECT_NAME);
            Object.DontDestroyOnLoad(host);
            host.AddComponent<CoopTestClient>().Run(arguments);
        }
    }
}
#endif
