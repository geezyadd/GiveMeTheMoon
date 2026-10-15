#if UNITY_INCLUDE_TESTS
using System;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // The loop tests read game services the way the scenes bind them: from the first scene context that has them.
    public static class LoopSmokeSceneContexts {
        public static T Resolve<T>() {
            foreach (SceneContext context in Object.FindObjectsByType<SceneContext>(FindObjectsSortMode.None)) {
                if (context.Container.HasBinding<T>())
                    return context.Container.Resolve<T>();
            }

            throw new InvalidOperationException($"No scene context binds {typeof(T).Name}.");
        }
    }
}
#endif
