using System.Collections.Generic;
using Features.EmoteModule.Data;
using Features.MvpModule;

namespace Features.EmoteModule.UI {
    public abstract class EmoteWheelViewBase : ViewBehaviour {
        // One sector per emote, the first at the top, then clockwise.
        public abstract void ShowEmotes(IReadOnlyList<EmoteEntry> emotes);

        // EmoteWheelRule.NoSector: none highlighted.
        public abstract void Highlight(int index);
    }
}
