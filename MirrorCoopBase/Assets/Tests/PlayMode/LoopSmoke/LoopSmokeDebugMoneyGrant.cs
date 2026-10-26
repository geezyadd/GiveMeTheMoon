#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Globalization;
using Features.CameraModule.Scripts.Models;
using Features.DebugModule.Contracts;
using Features.DebugModule.Scripts.Models;
using Features.DebugModule.Scripts.UI;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Generated;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.PlayMode.LoopSmoke {
    // The debug window's Shop money action, clicked as a player does: the corner button opens the panel, the "+1000"
    // button asks the server to add the configured amount to the crew wallet, the corner button closes the panel. The
    // normal run starts with little money, so the checks that spend more than that top the wallet up this way.
    public static class LoopSmokeDebugMoneyGrant {
        private const string LABEL_FORMAT = "+{0}";
        private const string SCREENSHOT = "debug-money-grant.png";

        public static IEnumerator GrantThroughDebugWindowCoroutine() {
            IReadOnlyWalletModel wallet = SessionQueries.Resolve<IReadOnlyWalletModel>();
            long amount = SessionQueries.Resolve<WalletConfiguration>().DebugGrantAmount;
            long expected = wallet.Balance + amount;
            DebugPanelModel panel = SessionQueries.Resolve<DebugPanelModel>();
            DebugToggleView toggle = Object.FindAnyObjectByType<DebugToggleView>();
            Assert.IsNotNull(toggle, "Money: no debug corner button in the session.");
            Assert.IsFalse(panel.IsOpen, "Money: the debug panel is open before the click.");

            toggle.DebugButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(panel.IsOpen, "Money: the corner button did not open the debug panel.");

            DebugActionRowItem row = FindRow(string.Format(CultureInfo.InvariantCulture, LABEL_FORMAT, amount));
            Assert.IsTrue(row.DebugButton.interactable, "Money: the debug money button is disabled in the session.");
            row.DebugButton.onClick.Invoke();
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => wallet.Balance == expected, $"the synced balance to grow by {amount}");
            // The Shop section is the panel's last one: scrolled down, the screenshot shows the clicked button.
            row.GetComponentInParent<ScrollRect>().verticalNormalizedPosition = 0f;
            yield return LoopSmokeReviveCheck.CaptureScreenshotCoroutine(SCREENSHOT);

            toggle.DebugButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(panel.IsOpen, "Money: the corner button did not close the debug panel.");
            Assert.IsFalse(SessionQueries.Resolve<CursorModel>().IsFreeCursorRequested, "Money: the closed debug panel still holds the cursor free.");
        }

        private static DebugActionRowItem FindRow(string label) {
            Assert.IsTrue(SessionQueries.Resolve<DebugActionsModel>().TryFind(DebugSections.SHOP, label, out DebugActionDefinition action), $"Money: no \"{label}\" debug action registered.");
            DebugPanelView view = Object.FindAnyObjectByType<DebugPanelView>();
            Assert.IsNotNull(view, "Money: no debug panel in the debug window.");
            Assert.IsTrue(view.TryGetDebugRow(action, out DebugActionRowItem row), $"Money: the debug panel shows no \"{label}\" button.");
            return row;
        }
    }
}
#endif
