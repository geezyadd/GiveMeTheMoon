using System;
using System.Collections.Generic;
using Features.MvpModule;

namespace Features.StagePresetModule.Scripts.UI {
    public abstract class StagePresetPickerViewBase : ViewBehaviour {
        // The index of the clicked option in the list given to SetOptions.
        public event Action<int> OnOptionClicked;

        public abstract void SetOptions(IReadOnlyList<string> labels);
        public abstract void SetSelected(int optionIndex, string description);
        public abstract void SetVisible(bool visible);

        protected void InvokeOptionClicked(int optionIndex) =>
            OnOptionClicked?.Invoke(optionIndex);
    }
}
