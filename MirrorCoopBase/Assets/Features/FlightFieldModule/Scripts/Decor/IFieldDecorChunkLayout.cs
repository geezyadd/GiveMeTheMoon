using System.Collections.Generic;
using Features.FlightFieldModule.Scripts.Generation;
using Features.FlightFieldModule.Scripts.Scene;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    public interface IFieldDecorChunkLayout {
        public void Fill(int fieldSeed, Vector2Int chunk, IFieldRandom random, List<FieldClusterRockPose> rocks);
    }
}
