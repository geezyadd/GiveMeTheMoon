using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Network;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    // What this peer has built of the field decor: the root, the field it shows and its standing chunks.
    public sealed class FieldDecorSceneModel {
        private readonly HashSet<Vector2Int> _builtChunks = new();

        public Transform Root { get; private set; }
        public bool HasRoot => Root != null;
        public int BuiltNumber { get; private set; } = LiveFieldNumbers.NO_FIELD;
        public IReadOnlyCollection<Vector2Int> BuiltChunks => _builtChunks;

        public void SetRoot(Transform root) =>
            Root = root;

        public void BeginBuild(int number) =>
            BuiltNumber = number;

        public bool IsChunkBuilt(Vector2Int chunk) =>
            _builtChunks.Contains(chunk);

        public void AddChunk(Vector2Int chunk) =>
            _builtChunks.Add(chunk);

        public void RemoveChunk(Vector2Int chunk) =>
            _builtChunks.Remove(chunk);

        public void Clear() {
            BuiltNumber = LiveFieldNumbers.NO_FIELD;
            _builtChunks.Clear();
        }
    }
}
