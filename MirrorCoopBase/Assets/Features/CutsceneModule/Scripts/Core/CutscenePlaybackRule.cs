using Features.CutsceneModule.Scripts.Network;
using UnityEngine;

namespace Features.CutsceneModule.Scripts.Core {
    public sealed class CutscenePlaybackRule : ICutscenePlaybackRule {
        public bool IsRunning(in CutscenePlayback playback, double now) =>
            playback.HasShots && !playback.IsSkipped && now < playback.EndTime;

        public bool ShouldStart(in CutscenePlayback playback, int lastPlayedSequence, double now) =>
            playback.Sequence != lastPlayedSequence && IsRunning(playback, now);

        public bool ShouldEnd(in CutscenePlayback playback, int playingSequence, double now) =>
            playback.Sequence != playingSequence || !IsRunning(playback, now);

        public float GetElapsedSeconds(in CutscenePlayback playback, double now) =>
            Mathf.Max(0f, (float)(now - playback.StartTime));
    }
}
