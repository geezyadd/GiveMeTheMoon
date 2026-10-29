using System;
using UnityEngine;

namespace Features.CutsceneModule.Scripts.Models {
    // This peer's own playing of the synced cutscene: whether it plays now, which sequence and the player's view it
    // started from. Local, never synced.
    public sealed class CutsceneLocalModel {
        public bool IsPlaying { get; private set; }
        public int PlayingSequence { get; private set; }
        // The last sequence this peer started, so a playback is played once.
        public int LastPlayedSequence { get; private set; }
        public Pose StartPose { get; private set; }

        public event Action OnPlayingChanged;

        public void Begin(int sequence, Pose startPose) {
            PlayingSequence = sequence;
            LastPlayedSequence = sequence;
            StartPose = startPose;
            if (IsPlaying)
                return;

            IsPlaying = true;
            OnPlayingChanged?.Invoke();
        }

        // The synced playback is gone (the ship left): the next run counts its sequences from the start again.
        public void Forget() {
            LastPlayedSequence = 0;
            End();
        }

        public void End() {
            if (!IsPlaying)
                return;

            IsPlaying = false;
            OnPlayingChanged?.Invoke();
        }
    }
}
