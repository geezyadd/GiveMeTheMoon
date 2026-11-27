using System.Collections.Generic;
using UnityEngine;

namespace Features.AudioModule {
    // The places on this peer where a music plays that the session music gives way to near it (a dancing player).
    // Local: the owner of such a music adds its source while the music plays and removes it when it stops.
    public sealed class MusicDuckSources {
        private readonly List<Transform> _sources = new();

        public IReadOnlyList<Transform> Sources => _sources;

        public void Add(Transform source) {
            if (_sources.Contains(source) == false)
                _sources.Add(source);
        }

        public void Remove(Transform source) =>
            _sources.Remove(source);
    }
}
