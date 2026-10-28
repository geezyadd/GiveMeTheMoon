#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using Features.DebugModule.Contracts;
using Features.DebugModule.Scripts.Models;
using Features.DebugModule.Scripts.UI;
using UnityEngine;

namespace Tests.Shared {
    // Clicks a debug window button as a player does: the corner button opens the panel, the action's row is clicked,
    // the corner button closes the panel again. For the coop client bot (no NUnit there): a failure is a message.
    public sealed class DebugWindowClicking {
        // Null while every step worked; otherwise what went wrong, and the click stopped there.
        public string Failure { get; private set; }

        // value: typed into the row's number field first (an action with a value), none to keep its default.
        public IEnumerator ClickCoroutine(string section, string label, string value = null) {
            Failure = null;
            DebugPanelModel panel = SessionQueries.Resolve<DebugPanelModel>();
            DebugToggleView toggle = Object.FindAnyObjectByType<DebugToggleView>();
            if (toggle == null) {
                Failure = "no debug corner button in the session";
                yield break;
            }

            toggle.DebugButton.onClick.Invoke();
            yield return null;
            if (panel.IsOpen == false) {
                Failure = "the corner button did not open the debug panel";
                yield break;
            }

            ClickRow(section, label, value);
            yield return null;
            toggle.DebugButton.onClick.Invoke();
            yield return null;
            if (Failure == null && panel.IsOpen)
                Failure = "the corner button did not close the debug panel";
        }

        private void ClickRow(string section, string label, string value) {
            if (SessionQueries.Resolve<DebugActionsModel>().TryFind(section, label, out DebugActionDefinition action) == false) {
                Failure = $"no \"{label}\" debug action registered in {section}";
                return;
            }

            if (Object.FindAnyObjectByType<DebugPanelView>().TryGetDebugRow(action, out DebugActionRowItem row) == false) {
                Failure = $"the debug panel shows no \"{label}\" button";
                return;
            }

            if (row.DebugButton.interactable == false) {
                Failure = $"\"{label}\" is disabled";
                return;
            }

            if (value != null)
                row.DebugValueField.text = value;

            row.DebugButton.onClick.Invoke();
        }
    }
}
#endif
