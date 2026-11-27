using UnityEngine;

namespace Features.CoroutineRunnerModule {
    // Lives in GlobalScene for the whole game; the MonoBehaviour StartCoroutine / StopCoroutine implement the interface.
    [DisallowMultipleComponent]
    public sealed class CoroutineRunner : MonoBehaviour, ICoroutineRunner {
    }
}
