using System.Collections.Generic;
using Features.MvpModule;

namespace Features.SettingsModule.UI.Focus {
    // Selects a window's default element (or the first one that can be selected) when the window gets the focus.
    public static class DefaultElementSelection {
        public static void Select(IEnumerable<IFocusableElement> elements) {
            SelectableFocusableElement first = null;
            foreach (IFocusableElement element in elements) {
                if (element is not SelectableFocusableElement selectable || selectable.CanBeSelected == false)
                    continue;

                if (selectable.IsDefault) {
                    selectable.Focus(null);
                    return;
                }

                first ??= selectable;
            }

            first?.Focus(null);
        }
    }
}
