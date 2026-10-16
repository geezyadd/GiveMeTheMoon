using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    public interface IFieldDecorSceneBuilder {
        public Transform CreateRoot();
        public void BuildChunk(Transform root, int fieldSeed, Vector2Int chunk);
        public void ReleaseChunk(Vector2Int chunk);
        public void BuildPlanets(Transform root, int fieldSeed);
        public void ReleaseAll();
    }
}
