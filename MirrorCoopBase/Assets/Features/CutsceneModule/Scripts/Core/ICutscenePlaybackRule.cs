using Features.CutsceneModule.Scripts.Network;

namespace Features.CutsceneModule.Scripts.Core {
    public interface ICutscenePlaybackRule {
        // The playback has shots, is not skipped and its shots are not over at the network time.
        public bool IsRunning(in CutscenePlayback playback, double now);
        // A peer that has not played this sequence yet starts it.
        public bool ShouldStart(in CutscenePlayback playback, int lastPlayedSequence, double now);
        // A peer playing a sequence ends it at the end of its shots, on a skip or when another one replaced it.
        public bool ShouldEnd(in CutscenePlayback playback, int playingSequence, double now);
        public float GetElapsedSeconds(in CutscenePlayback playback, double now);
    }
}
