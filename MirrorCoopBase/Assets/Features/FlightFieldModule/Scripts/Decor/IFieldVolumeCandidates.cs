using Features.FlightFieldModule.Scripts.Configuration;
using UnityEngine;

namespace Features.FlightFieldModule.Scripts.Decor {
    public interface IFieldVolumeCandidates {
        // The grid point (x across, y = layer, z along) of a band on one side of the plane (+1 above, -1 below),
        // from the field seed and the point only.
        public FieldVolumeCandidate Get(FieldVolumeConfiguration.Band band, int bandIndex, int side, Vector3Int point, int fieldSeed, FieldVolumeSource source);
        // The layers of a band: its height range cut into slices about one grid spacing high.
        public int CountLayers(FieldVolumeConfiguration.Band band);
    }
}
