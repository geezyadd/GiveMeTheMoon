using System.Collections.Generic;
using Features.CutsceneModule.Contracts;

namespace Features.PointOfInterestModule.Scripts.Editor.Tests {
    public sealed class RecordingCutsceneDirector : ICutsceneDirector {
        public bool IsPlaying => Played > 0;
        public int Played { get; private set; }
        public List<CutsceneShot> LastShots { get; } = new();

        public void ServerPlay(IReadOnlyList<CutsceneShot> shots) {
            Played++;
            LastShots.Clear();
            LastShots.AddRange(shots);
        }

        public void ServerSkip() { }
    }
}
