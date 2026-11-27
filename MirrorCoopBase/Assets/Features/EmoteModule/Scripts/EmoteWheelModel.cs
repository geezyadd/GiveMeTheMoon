using System;

namespace Features.EmoteModule {
    // The local player's emote wheel: whether it is open, the sector under the pointer, and the emote picked on release.
    // Written by EmoteWheelSystem only; the wheel's presenter and the player's emote plays read it.
    public sealed class EmoteWheelModel {
        public bool IsOpen { get; private set; }

        // EmoteWheelRule.NoSector while the pointer is in the dead zone.
        public int HighlightedIndex { get; private set; } = EmoteWheelRule.NoSector;

        public event Action OnOpenChanged;
        public event Action OnHighlightChanged;

        // The catalog index of the emote the local player picked.
        public event Action<int> OnEmoteChosen;

        public void Open() {
            if (IsOpen)
                return;

            IsOpen = true;
            Highlight(EmoteWheelRule.NoSector);
            OnOpenChanged?.Invoke();
        }

        public void Highlight(int index) {
            if (HighlightedIndex == index)
                return;

            HighlightedIndex = index;
            OnHighlightChanged?.Invoke();
        }

        public void Close() {
            if (IsOpen == false)
                return;

            IsOpen = false;
            OnOpenChanged?.Invoke();
        }

        public void Choose(int index) =>
            OnEmoteChosen?.Invoke(index);
    }
}
