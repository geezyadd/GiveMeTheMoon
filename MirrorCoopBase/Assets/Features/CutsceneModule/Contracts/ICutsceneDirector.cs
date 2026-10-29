using System.Collections.Generic;

namespace Features.CutsceneModule.Contracts {
    // Server: plays a cutscene for every player at once (each peer moves its own camera along the same shots at the same
    // network time, blocks its player's controls and hides its HUD) and skips it for everyone.
    public interface ICutsceneDirector {
        public bool IsPlaying { get; }
        public void ServerPlay(IReadOnlyList<CutsceneShot> shots);
        public void ServerSkip();
    }
}
