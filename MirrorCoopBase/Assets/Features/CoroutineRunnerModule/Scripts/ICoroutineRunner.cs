using System.Collections;
using UnityEngine;

namespace Features.CoroutineRunnerModule {
    // The one place coroutines run from, so plain classes (services, systems) can run them too.
    public interface ICoroutineRunner {
        Coroutine StartCoroutine(IEnumerator routine);
        void StopCoroutine(Coroutine coroutine);
    }
}
