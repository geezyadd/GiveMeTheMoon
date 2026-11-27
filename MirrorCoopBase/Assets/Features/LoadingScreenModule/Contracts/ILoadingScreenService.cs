using System;

namespace Features.LoadingScreenModule.Contracts {
    // The screen that covers scene changes. FadeIn covers the screen, FadeOut uncovers it; each calls its action (may be
    // null) once done. A call during the opposite fade (or its delay) turns it around from where it is.
    public interface ILoadingScreenService {
        bool IsCovered { get; }
        void FadeIn(Action onComplete = null);
        void FadeOut(Action onComplete = null);
        // Keeps the screen as it is for delaySeconds, then uncovers it: time for what a new scene spawns to settle.
        void FadeOut(float delaySeconds, Action onComplete = null);
    }
}
