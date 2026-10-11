#if UNITY_INCLUDE_TESTS
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Features.CameraModule.Scripts;
using Features.CharacterMovableModule.Scripts;
using Features.CharacterMovableModule.Scripts.Models;
using Features.PlayerLifeModule.Scripts;
using Features.PlayerLifeModule.Scripts.Spectator;
using Features.ShipModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Core;
using Features.ShopModule.Scripts.Data;
using Features.ShopModule.Scripts.Generated;
using Features.ShopModule.Scripts.Network;
using Features.ShopModule.Scripts.UI;
using Game.Connection;
using Mirror;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // Buying back a dead crewmate at a station with a second player (a bot on the host): the host buys the bot back
    // through the kiosk window, twice in one frame and once without money, then dies and is bought back by the bot,
    // and must stand on the deck, alive and in control, without a jump on the way. At a station nobody rides the ship
    // yet: the next takeoff boards the revived host like everyone on the deck (LoopSmokeTest checks it in cruise).
    public sealed class LoopSmokeReviveCheck {
        private const float BOT_BESIDE_KIOSK = 2f;
        private const float STAND_HEIGHT = 0.8f;
        private const float REJECTED_COMMAND_WAIT_SECONDS = 0.5f;
        private const float SPECTATOR_SETTLE_SECONDS = 0.5f;
        private const float REVIVE_SETTLE_SECONDS = 1.5f;
        private const float SPAWN_TOLERANCE = 0.1f;
        private const float DECK_TOLERANCE = 0.5f;
        private const float MAX_FRAME_STEP = 0.3f;
        private const float MIN_SPAWN_SPACING = 1f;
        private const string SCREENSHOT_FOLDER = "Temp/LoopSmoke";
        private const string CREW_SCREENSHOT = "revive-crew-section.png";
        private const string DECK_SCREENSHOT = "revive-host-on-deck.png";

        private readonly ShipBase _ship;
        private readonly IReadOnlyWalletModel _wallet = LoopSmokeShopCheck.Resolve<IReadOnlyWalletModel>();
        private readonly IPlayerLifeQuery _lifeQuery = LoopSmokeShopCheck.Resolve<IPlayerLifeQuery>();
        private readonly long _price = LoopSmokeShopCheck.Resolve<ReviveConfiguration>().RevivePrice;

        public LoopSmokeReviveCheck(ShipBase ship) =>
            _ship = ship;

        private static NetworkIdentity Player => NetworkClient.localPlayer;
        private static CrewWallet Wallet => Object.FindAnyObjectByType<CrewWallet>();

        public IEnumerator BuyBackCrewCoroutine(ShipLandingPad pad, ConnectionNetworkManager manager) {
            ShopKioskInteractable kiosk = pad.GetComponentInChildren<ShopKioskInteractable>();
            ShopModel shop = LoopSmokeShopCheck.Resolve<ShopModel>();
            LoopSmokeBot bot = LoopSmokeBot.Join(manager);
            try {
                CameraLookDriver.DebugPitchOverride = true;
                yield return LoopSmokeShopCheck.WaitForCoroutine(() => bot.Identity != null && bot.Identity.isClient, "the bot player spawned on the host client");
                yield return BuyBackBotAtKioskCoroutine(bot, kiosk, shop);
                yield return AssertRefusedWithoutMoneyCoroutine(bot);
                shop.Close();
                yield return BuyBackHostAsBotCoroutine(bot, kiosk);
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
                bot.Leave();
            }

            yield return LoopSmokeShopCheck.WaitForCoroutine(() => bot.Identity == null, "the bot player removed");
        }

        private IEnumerator BuyBackBotAtKioskCoroutine(LoopSmokeBot bot, ShopKioskInteractable kiosk, ShopModel shop) {
            PlaceBeside(bot, kiosk);
            bot.Identity.GetComponent<IDamageable>().ServerKill(DamageType.Generic);
            yield return LoopSmokeShopCheck.OpenAtKioskCoroutine(kiosk, shop);
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => FindCrewRows().Count == 1, "one row in the shop's Crew section");
            CrewReviveRow row = FindCrewRows()[0];
            Assert.IsTrue(HasLabel(row, LoopSmokeBot.NAME), $"The Crew row does not show the bot's name {LoopSmokeBot.NAME}.");
            Button buyBack = row.GetComponentInChildren<Button>();
            Assert.IsTrue(buyBack.interactable, "The buy-back button is disabled with enough money.");
            yield return CaptureScreenshotCoroutine(CREW_SCREENSHOT);

            long balance = _wallet.Balance;
            // Two clicks in one frame, as two players pressing at once: only the first one may be paid for.
            buyBack.onClick.Invoke();
            buyBack.onClick.Invoke();
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => bot.Life.LifeState == PlayerLifeState.Alive, "the bot bought back");
            yield return new WaitForSeconds(REJECTED_COMMAND_WAIT_SECONDS);
            Assert.AreEqual(balance - _price, _wallet.Balance, "A double buy-back did not take the price exactly once.");
            Assert.Less(DistanceToNearestSpawnPoint(bot.Identity.transform.position), SPAWN_TOLERANCE, "The bought-back bot is not at a deck spawn point.");
            Assert.AreEqual(0, FindCrewRows().Count, "The Crew section still lists the bought-back bot.");
        }

        private IEnumerator AssertRefusedWithoutMoneyCoroutine(LoopSmokeBot bot) {
            long balance = _wallet.Balance;
            bot.Identity.GetComponent<IDamageable>().ServerKill(DamageType.Generic);
            Wallet.ServerSetBalance(_price - 1);
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => FindCrewRows().Count == 1, "the dead bot listed again");
            Button buyBack = FindCrewRows()[0].GetComponentInChildren<Button>();
            Assert.IsFalse(buyBack.interactable, "The buy-back button is enabled without enough money.");

            buyBack.onClick.Invoke();
            yield return new WaitForSeconds(REJECTED_COMMAND_WAIT_SECONDS);
            Assert.AreEqual(PlayerLifeState.Dead, bot.Life.LifeState, "The server revived the bot without enough money.");
            Assert.AreEqual(_price - 1, _wallet.Balance, "The server took money for a refused buy-back.");

            Wallet.ServerSetBalance(balance);
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => buyBack.interactable, "the buy-back button enabled with the money back");
            Vector3 firstSpawn = bot.Identity.transform.position;
            buyBack.onClick.Invoke();
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => bot.Life.LifeState == PlayerLifeState.Alive, "the bot bought back again");
            Vector3 secondSpawn = bot.Identity.transform.position;
            Assert.Less(DistanceToNearestSpawnPoint(secondSpawn), SPAWN_TOLERANCE, $"The bot bought back again at {secondSpawn} is not at a deck spawn point: {DescribeSpawnPoints()}.");
            Assert.Greater(Vector3.Distance(firstSpawn, secondSpawn), MIN_SPAWN_SPACING, "Two buy-backs in a row used the same deck spawn point.");
        }

        private IEnumerator BuyBackHostAsBotCoroutine(LoopSmokeBot bot, ShopKioskInteractable kiosk) {
            SpectatorModel spectator = LoopSmokeShopCheck.Resolve<SpectatorModel>();
            PlayerLifeBody host = Player.GetComponent<PlayerLifeBody>();
            host.CmdDebugKill();
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => _lifeQuery.LocalState == PlayerLifeState.Dead, "the host dead");
            yield return new WaitForSeconds(SPECTATOR_SETTLE_SECONDS);
            Assert.IsTrue(spectator.IsSpectating, "The dead host does not spectate.");

            PlaceBeside(bot, kiosk);
            bot.Customer.ServerOpenShop(kiosk);
            long balance = _wallet.Balance;
            LoopSmokeShopCheck.Resolve<IReviveShopSystem>().ServerRevive(Wallet, host.Key, bot.Customer);
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => _lifeQuery.LocalState == PlayerLifeState.Alive, "the host bought back by the bot");
            Assert.AreEqual(balance - _price, _wallet.Balance, "Buying back the host did not take the price.");

            float largestStep = 0f;
            yield return TrackLargestStepCoroutine(step => largestStep = Mathf.Max(largestStep, step));
            Assert.Less(largestStep, MAX_FRAME_STEP, $"The revived host jumped {largestStep} m in one frame.");
            Assert.IsTrue(IsOnDeck(Player.transform.position), $"The revived host at {Player.transform.position} is not on the deck {_ship.DeckGeometry.Bounds}.");
            Assert.IsFalse(spectator.IsSpectating, "The revived host still spectates.");
            Assert.IsFalse(LoopSmokeShopCheck.Resolve<PlayerControlBlockModel>().IsBlocked, "The revived host cannot move.");
            yield return CaptureScreenshotCoroutine(DECK_SCREENSHOT);
        }

        private static IEnumerator TrackLargestStepCoroutine(Action<float> onStep) {
            Vector3 previous = Player.transform.position;
            float until = Time.time + REVIVE_SETTLE_SECONDS;
            while (Time.time < until) {
                yield return null;
                Vector3 current = Player.transform.position;
                onStep(Vector3.Distance(previous, current));
                previous = current;
            }
        }

        private bool IsOnDeck(Vector3 position) {
            Bounds deck = _ship.DeckGeometry.Bounds;
            return position.x >= deck.min.x - DECK_TOLERANCE && position.x <= deck.max.x + DECK_TOLERANCE
                && position.z >= deck.min.z - DECK_TOLERANCE && position.z <= deck.max.z + DECK_TOLERANCE
                && position.y > deck.max.y;
        }

        private static float DistanceToNearestSpawnPoint(Vector3 position) {
            float nearest = float.MaxValue;
            foreach (ShipDeckSpawnPoint point in Object.FindObjectsByType<ShipDeckSpawnPoint>(FindObjectsSortMode.None))
                nearest = Mathf.Min(nearest, Vector3.Distance(point.transform.position, position));

            return nearest;
        }

        private static string DescribeSpawnPoints() {
            List<string> points = new List<string>();
            foreach (ShipDeckSpawnPoint point in Object.FindObjectsByType<ShipDeckSpawnPoint>(FindObjectsSortMode.None))
                points.Add($"{point.name} {point.transform.position}");

            return string.Join(", ", points);
        }

        private static void PlaceBeside(LoopSmokeBot bot, ShopKioskInteractable kiosk) {
            Vector3 position = kiosk.transform.position + kiosk.transform.right * BOT_BESIDE_KIOSK + Vector3.up * STAND_HEIGHT;
            Rigidbody body = bot.Identity.GetComponent<CharacterMovableBase>().Body;
            body.position = position;
            body.transform.position = position;
        }

        private static List<CrewReviveRow> FindCrewRows() {
            List<CrewReviveRow> active = new List<CrewReviveRow>();
            foreach (CrewReviveRow row in Object.FindObjectsByType<CrewReviveRow>(FindObjectsSortMode.None)) {
                if (row.isActiveAndEnabled)
                    active.Add(row);
            }

            return active;
        }

        private static bool HasLabel(CrewReviveRow row, string text) {
            foreach (Text label in row.GetComponentsInChildren<Text>()) {
                if (label.text == text)
                    return true;
            }

            return false;
        }

        // Kept in the project's Temp folder for a human to look at; the test does not compare images.
        private static IEnumerator CaptureScreenshotCoroutine(string fileName) {
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SCREENSHOT_FOLDER));
            Directory.CreateDirectory(folder);
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, fileName));
            yield return null;
            yield return null;
        }
    }
}
#endif
