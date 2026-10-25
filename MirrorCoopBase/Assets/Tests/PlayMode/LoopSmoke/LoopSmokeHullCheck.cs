#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Globalization;
using Features.CameraModule.Scripts.Models;
using Features.DebugModule.Contracts;
using Features.DebugModule.Scripts.Models;
using Features.DebugModule.Scripts.UI;
using Features.ShipModule.Scripts.Generated;
using Features.ShipModule.Scripts.Hull;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // The hull through the debug window, clicked as a player does: the corner button opens the panel (and frees the
    // cursor), the "Hit the hull" button with the damage typed in lowers the synced hull by the armor-reduced damage in
    // cruise, the corner button closes the panel again; the landing repairs the hull.
    public sealed class LoopSmokeHullCheck {
        private const float HIT = 15f;
        private const float TOLERANCE = 0.01f;
        private const string HIT_LABEL = "Hit the hull";

        public IEnumerator HitInCruiseCoroutine() {
            IReadOnlyShipStatsModel stats = SessionQueries.Resolve<IReadOnlyShipStatsModel>();
            Assert.Greater(stats.HullMax, 0f, "Hull: the synced hull max is 0 in cruise.");
            Assert.AreEqual(stats.HullMax, stats.Hull, TOLERANCE, "Hull: the run did not start with a full hull.");
            float expected = stats.Hull - SessionQueries.Resolve<IShipHullDamageRule>().CalculateDealtDamage(HIT, stats.Armor);

            yield return HitThroughDebugWindowCoroutine();

            Assert.Less(stats.Hull, stats.HullMax, "Hull: a debug hit in cruise did not lower the synced hull.");
            Assert.AreEqual(expected, stats.Hull, TOLERANCE, "Hull: the synced hull after the hit is not the armor-reduced damage.");
        }

        public void AssertRepairedAfterLanding(int loop) {
            IReadOnlyShipStatsModel stats = SessionQueries.Resolve<IReadOnlyShipStatsModel>();
            Assert.AreEqual(stats.HullMax, stats.Hull, TOLERANCE, $"Loop {loop}: the hull was not repaired on landing.");
        }

        private static IEnumerator HitThroughDebugWindowCoroutine() {
            DebugPanelModel panel = SessionQueries.Resolve<DebugPanelModel>();
            CursorModel cursor = SessionQueries.Resolve<CursorModel>();
            DebugToggleView toggle = Object.FindAnyObjectByType<DebugToggleView>();
            Assert.IsNotNull(toggle, "Hull: no debug corner button in the session.");
            Assert.IsFalse(panel.IsOpen, "Hull: the debug panel is open before the click.");

            toggle.DebugButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(panel.IsOpen, "Hull: the corner button did not open the debug panel.");
            Assert.IsTrue(cursor.IsFreeCursorRequested, "Hull: the open debug panel did not free the cursor.");

            DebugActionRowItem row = FindRow(DebugSections.SHIP, HIT_LABEL);
            Assert.IsTrue(row.DebugButton.interactable, $"Hull: \"{HIT_LABEL}\" is disabled for the host in cruise.");
            row.DebugValueField.text = HIT.ToString(CultureInfo.InvariantCulture);
            row.DebugButton.onClick.Invoke();
            yield return null;

            toggle.DebugButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(panel.IsOpen, "Hull: the corner button did not close the debug panel.");
            Assert.IsFalse(cursor.IsFreeCursorRequested, "Hull: the closed debug panel still holds the cursor free.");
        }

        private static DebugActionRowItem FindRow(string section, string label) {
            Assert.IsTrue(SessionQueries.Resolve<DebugActionsModel>().TryFind(section, label, out DebugActionDefinition action), $"Hull: no \"{label}\" debug action registered.");
            DebugPanelView view = Object.FindAnyObjectByType<DebugPanelView>();
            Assert.IsNotNull(view, "Hull: no debug panel in the debug window.");
            Assert.IsTrue(view.TryGetDebugRow(action, out DebugActionRowItem row), $"Hull: the debug panel shows no \"{label}\" button.");
            return row;
        }
    }
}
#endif
