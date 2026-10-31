using System;
using Features.CutsceneModule.Contracts;

namespace Features.CutsceneModule.Scripts.Network {
    // The cutscene every peer plays: its shots from StartTime (network time) on for ShotsSeconds, then the camera
    // blends back to the player. A skip keeps the sequence and marks it skipped. Two playbacks are the same when
    // their sequence and skip are.
    [Serializable]
    public struct CutscenePlayback : IEquatable<CutscenePlayback> {
        public int Sequence;
        public double StartTime;
        public float ShotsSeconds;
        public bool IsSkipped;
        public CutsceneShot[] Shots;

        public bool HasShots => Shots != null && Shots.Length > 0;
        public double EndTime => StartTime + ShotsSeconds;

        public bool Equals(CutscenePlayback other) =>
            Sequence == other.Sequence && IsSkipped == other.IsSkipped;

        public override bool Equals(object other) =>
            other is CutscenePlayback playback && Equals(playback);

        public override int GetHashCode() =>
            HashCode.Combine(Sequence, IsSkipped);
    }
}
