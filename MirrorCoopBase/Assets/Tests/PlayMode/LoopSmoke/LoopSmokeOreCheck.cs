#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Features.CameraModule.Scripts;
using Features.CameraModule.Scripts.Services;
using Features.CharacterMovableModule.Scripts;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Ore;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.NetworkModelModule.Scripts;
using Features.ShopModule.Scripts.Configurations;
using Features.ShopModule.Scripts.Generated;
using Features.WeaponModule.Scripts.Readiness;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // Ore and the harpoon the way a crew earns money: at the station before flight 1 of run 2 the host buys the harpoon
    // at the kiosk and picks it up by hand; in that cruise rocks are broken (the debug break, as a bullet breaks them)
    // until one drops ore, the piece stands in the live field and on the host's screen, the host aims the camera at it
    // through the look input and clicks the fire button: the rope hooks it (the host's key, the rope line shown), it
    // is pulled in, reaches the ship once and the synced crew balance grows by exactly the prices delivered. Then ore
    // nobody picks up is gone from the field and the screen at its end of life (LoopSmokeOreConfigOverride keeps it
    // short). The host drops the harpoon, and the test removes it before the landing (the first landing of run 2 must
    // spawn what the first landing of run 1 did, LoopSmokeRunLeakCheck).
    public sealed class LoopSmokeOreCheck {
        private const string HARPOON_ID = "harpoon";
        private const string DROP_SCREENSHOT = "ore-drop.png";
        private const string ROPE_SCREENSHOT = "ore-rope.png";
        private const string DELIVERY_SCREENSHOT = "ore-delivered.png";
        private const int MAX_ROCK_BREAKS = 25;
        private const int SHOT_ATTEMPTS = 3;
        private const float PULL_DISTANCE = 30f;
        private const float PULL_HEIGHT_BELOW = 1f;
        private const float SETTLE_SECONDS = 0.4f;
        private const float AIM_TIMEOUT_SECONDS = 3f;
        private const float HOOK_TIMEOUT_SECONDS = 1.5f;
        private const float DELIVERY_TIMEOUT_SECONDS = 8f;
        private const float COOLDOWN_TIMEOUT_SECONDS = 2.5f;
        private const float EXPIRY_MARGIN_SECONDS = 2f;
        private const float COMMAND_TIMEOUT_SECONDS = 3f;
        private const float BOARD_DROP_HEIGHT = 0.5f;
        private const float BOARD_SETTLE_SECONDS = 0.6f;
        private const float READY = 1f;
        private const string SCREENSHOT_FOLDER = "Temp/LoopSmoke";
        private const string VIEW_SCREENSHOT_PREFIX = "view-";
        private const int VIEW_SCREENSHOT_WIDTH = 1920;
        private const int VIEW_SCREENSHOT_HEIGHT = 1080;
        private const int VIEW_SCREENSHOT_DEPTH = 24;
        private const string PHOTO_CAMERA_NAME = "LoopSmokeOrePhotoCamera";
        // The photo camera stands beside and above the player's view, so the player's own body does not hide the ore.
        private static readonly Vector3 _photoOffset = new Vector3(2.5f, 1.2f, -1.5f);

        private readonly LoopSmokeSession _session;
        private readonly LoopSmokeFlight _flight;
        private readonly List<FieldOreDescription> _ores = new List<FieldOreDescription>();
        private readonly List<FieldOreCollected> _collected = new List<FieldOreCollected>();
        private readonly List<FieldOreId> _dropped = new List<FieldOreId>();

        // The pieces the last BreakRocksUntilOreCoroutine saw drop.
        public IReadOnlyList<FieldOreId> DroppedOre => _dropped;

        private static NetworkIdentity Player => NetworkClient.localPlayer;
        private static GrabController Hand => Player.GetComponent<GrabController>();

        public LoopSmokeOreCheck(LoopSmokeContext context) {
            _session = context.Session;
            _flight = context.Flight;
        }

        // After the crew invite check of this station (order 0).
        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, 2, 1, 20)]
        public IEnumerator BuyAndPickUpHarpoonCoroutine() {
            HandWeaponDefinition harpoon = SessionQueries.Resolve<ShopCatalog>().HandWeapons.First(handWeapon => handWeapon.Id == HARPOON_ID);
            try {
                CameraLookDriver.DebugPitchOverride = true;
                HandWeaponItem bought = null;
                yield return LoopSmokeKiosk.BuyCoroutine<HandWeaponItem>(_flight, LoopSmokeKiosk.FindHandWeaponEntry(HARPOON_ID), harpoon.Price, IsLooseHarpoon, item => bought = item);
                yield return LoopSmokeKiosk.PickUpCoroutine(bought.GetComponent<Grabbable>());
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }

            HeldWeaponReadinessModel readiness = SessionQueries.Resolve<HeldWeaponReadinessModel>();
            yield return _session.WaitForCoroutine(() => readiness.IsHolding, COMMAND_TIMEOUT_SECONDS, "Harpoon: the HUD shows the harpoon in hand");
            ShipDeckBoarding.PutAbove(Player.GetComponent<CharacterMovableBase>(), _session.Ship, LoopSmokeTurretCheck.GunnerSpot, BOARD_DROP_HEIGHT);
            yield return new WaitForSeconds(BOARD_SETTLE_SECONDS);
            Assert.IsTrue(SessionQueries.IsOnDeck(_session.Ship, Player.transform.position), "Harpoon: the host did not board.");
        }

        // After the crew invite check of this cruise (order 10).
        [LoopSmokeStep(LoopSmokePhase.InCruise, 2, 1, 20)]
        public IEnumerator PullOreInAndLetOreRunOutCoroutine() {
            Assert.IsTrue(Hand.Held != null && Hand.Held.TryGetComponent(out HandWeaponItem _), "Harpoon: the host does not hold the harpoon in cruise.");
            Vector3 start = _session.Ship.transform.position;
            try {
                CameraLookDriver.DebugPitchOverride = true;
                yield return BreakRocksUntilOreCoroutine();
                yield return PullOreInCoroutine(_dropped[0]);
                yield return PutShipBackFacingDestinationCoroutine(start);
                yield return BreakRocksUntilOreCoroutine();
                yield return WaitForOreToRunOutCoroutine();
                _flight.FaceDestination();
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }

            HandWeaponAiming.TurnViewBack();
            yield return null;
            Player.GetComponent<LocalPlayerInteraction>().ReleaseHeld();
            HeldWeaponReadinessModel readiness = SessionQueries.Resolve<HeldWeaponReadinessModel>();
            yield return _session.WaitForCoroutine(() => Hand.IsHolding == false && readiness.IsHolding == false, COMMAND_TIMEOUT_SECONDS, "Harpoon: dropped, the hand and the HUD ring empty");
            RemoveDroppedHarpoon();
        }

        private static void RemoveDroppedHarpoon() {
            foreach (HandWeaponItem item in Object.FindObjectsByType<HandWeaponItem>(FindObjectsSortMode.None))
                NetworkServer.Destroy(item.gameObject);
        }

        // The debug move back lands in the flight's next step: the heading to the destination is taken from there, or
        // the ship would fly on across the field and never reach the station of this short test flight.
        private IEnumerator PutShipBackFacingDestinationCoroutine(Vector3 start) {
            yield return _flight.PutShipBackCoroutine(start);
            yield return new WaitForFixedUpdate();
            yield return null;
            _flight.FaceDestination();
        }

        // Breaks the rock nearest to the ship, then the next, until one drops ore that is still out in the field (also
        // the coop harpoon test's host step).
        public IEnumerator BreakRocksUntilOreCoroutine() {
            IFieldRockBreakDebug breaker = SessionQueries.Resolve<IFieldRockBreakDebug>();
            IFieldOreQuery query = SessionQueries.Resolve<IFieldOreQuery>();
            _dropped.Clear();
            for (int breaks = 0; breaks < MAX_ROCK_BREAKS && _dropped.Count == 0; breaks++) {
                query.FindAll(_ores);
                HashSet<FieldOreId> before = new HashSet<FieldOreId>(_ores.Select(ore => ore.Id));
                FieldRockBreakResult result = breaker.DebugBreakNearestRock(out FieldRockId rock);
                Assert.AreEqual(FieldRockBreakResult.Broken, result, $"Ore: the debug break of the nearest rock gave {result}.");
                yield return null;
                query.FindAll(_ores);
                _dropped.AddRange(_ores.Where(ore => !before.Contains(ore.Id) && !ore.IsHooked).Select(ore => ore.Id));
                if (_dropped.Count > 0)
                    Debug.Log($"[LoopSmoke] ore: rock {rock} dropped {_dropped.Count} piece(s) after {breaks + 1} break(s)");
            }

            Assert.IsNotEmpty(_dropped, $"Ore: none of {MAX_ROCK_BREAKS} broken rocks dropped ore.");
            yield return _session.WaitForCoroutine(() => IsShown(_dropped[0]), COMMAND_TIMEOUT_SECONDS, $"Ore: {_dropped[0]} shown on the host");
        }

        private IEnumerator PullOreInCoroutine(FieldOreId id) {
            IFieldOreQuery query = SessionQueries.Resolve<IFieldOreQuery>();
            IFieldOreEvents events = SessionQueries.Resolve<IFieldOreEvents>();
            IReadOnlyWalletModel wallet = SessionQueries.Resolve<IReadOnlyWalletModel>();
            PlayerKey host = SessionQueries.Resolve<IPlayerIdentityService>().GetKey(NetworkServer.localConnection);
            Assert.IsTrue(query.TryGetOre(id, out FieldOreDescription ore), $"Ore: {id} is gone before the pull.");
            _flight.PutShipBeside(ore.Center, PULL_DISTANCE, PULL_HEIGHT_BELOW);
            yield return new WaitForSeconds(SETTLE_SECONDS);
            yield return AimViewAtCoroutine(id);
            yield return LoopSmokeReviveCheck.CaptureScreenshotCoroutine(DROP_SCREENSHOT);
            yield return CaptureViewCoroutine(DROP_SCREENSHOT, CurrentCenter(id));

            long balance = wallet.Balance;
            _collected.Clear();
            events.OreCollected += _collected.Add;
            try {
                yield return ThrowUntilHookedCoroutine(id, host);
                Assert.IsTrue(IsRopeShown(id), $"Ore: the host does not show the rope to {id}.");
                yield return LoopSmokeReviveCheck.CaptureScreenshotCoroutine(ROPE_SCREENSHOT);
                yield return CaptureViewCoroutine(ROPE_SCREENSHOT, CurrentCenter(id));
                yield return _session.WaitForCoroutine(() => _collected.Any(collected => collected.Id.Equals(id)), DELIVERY_TIMEOUT_SECONDS, $"Ore: {id} pulled in to the ship");
                yield return new WaitForSeconds(SETTLE_SECONDS);
            } finally {
                events.OreCollected -= _collected.Add;
            }

            Assert.AreEqual(1, _collected.Count(collected => collected.Id.Equals(id)), $"Ore: {id} reached the ship more than once.");
            Assert.AreEqual(host, _collected.First(collected => collected.Id.Equals(id)).Hooker, "Ore: the delivered piece was not on the host's rope.");
            Assert.AreEqual(balance + _collected.Sum(collected => collected.Price), wallet.Balance, "Ore: the crew balance did not grow by exactly the delivered prices.");
            Assert.IsFalse(query.TryGetOre(id, out _), $"Ore: {id} is still in the field after it reached the ship.");
            Assert.IsFalse(IsShown(id), $"Ore: {id} is still shown after it reached the ship.");
            yield return LoopSmokeReviveCheck.CaptureScreenshotCoroutine(DELIVERY_SCREENSHOT);
            yield return CaptureViewCoroutine(DELIVERY_SCREENSHOT, _session.Ship.transform.position);
            Debug.Log($"[LoopSmoke] ore: the harpoon pulled {id} in, the balance grew {balance} -> {wallet.Balance}");
        }

        // The piece drifts and the rope may meet a rock first, as a player's may: a miss is tried again after the reload.
        private IEnumerator ThrowUntilHookedCoroutine(FieldOreId id, PlayerKey host) {
            IFieldOreQuery query = SessionQueries.Resolve<IFieldOreQuery>();
            HeldWeaponReadinessModel readiness = SessionQueries.Resolve<HeldWeaponReadinessModel>();
            for (int attempt = 1; IsHookedBy(query, id, host) == false; attempt++) {
                if (attempt > SHOT_ATTEMPTS)
                    Assert.Fail($"Ore: none of {SHOT_ATTEMPTS} ropes hooked {id} (readiness {readiness.Readiness:0.00}).");

                yield return AimViewAtCoroutine(id);
                yield return HandWeaponAiming.ClickFireCoroutine();
                Assert.Less(readiness.Readiness, READY, $"Ore: click {attempt} on a ready harpoon threw no rope.");
                for (float waited = 0f; waited < HOOK_TIMEOUT_SECONDS && IsHookedBy(query, id, host) == false; waited += Time.deltaTime)
                    yield return null;

                if (IsHookedBy(query, id, host) == false)
                    yield return _session.WaitForCoroutine(() => readiness.Readiness >= READY, COOLDOWN_TIMEOUT_SECONDS, "Ore: the harpoon reloaded after a miss");
            }
        }

        private IEnumerator WaitForOreToRunOutCoroutine() {
            IFieldOreQuery query = SessionQueries.Resolve<IFieldOreQuery>();
            List<FieldOreId> pieces = new List<FieldOreId>(_dropped);
            float timeout = LoopSmokeOreConfigOverride.ORE_LIFETIME_SECONDS + EXPIRY_MARGIN_SECONDS;
            yield return _session.WaitForCoroutine(() => pieces.All(id => query.TryGetOre(id, out _) == false), timeout, $"Ore: {pieces.Count} unpicked piece(s) gone at their end of life");
            yield return null;
            Assert.IsTrue(pieces.All(id => IsShown(id) == false), "Ore: a piece that ran out is still shown on the host.");
            Debug.Log($"[LoopSmoke] ore: {pieces.Count} unpicked piece(s) ran out");
        }

        // A test photo at 1920 x 1080 next to the screen capture: a camera like the game's, beside the player's view,
        // looking at the piece (no overlay HUD in it).
        private static IEnumerator CaptureViewCoroutine(string fileName, Vector3 focus) {
            yield return new WaitForEndOfFrame();
            Camera game = SessionQueries.Resolve<IGameCameraService>().OutputCamera;
            Camera photo = new GameObject(PHOTO_CAMERA_NAME).AddComponent<Camera>();
            photo.CopyFrom(game);
            photo.transform.position = game.transform.TransformPoint(_photoOffset);
            photo.transform.LookAt(focus);
            RenderTexture target = RenderTexture.GetTemporary(VIEW_SCREENSHOT_WIDTH, VIEW_SCREENSHOT_HEIGHT, VIEW_SCREENSHOT_DEPTH);
            photo.targetTexture = target;
            photo.Render();
            photo.targetTexture = null;
            Object.Destroy(photo.gameObject);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = target;
            Texture2D picture = new Texture2D(VIEW_SCREENSHOT_WIDTH, VIEW_SCREENSHOT_HEIGHT, TextureFormat.RGB24, false);
            picture.ReadPixels(new Rect(0, 0, VIEW_SCREENSHOT_WIDTH, VIEW_SCREENSHOT_HEIGHT), 0, 0);
            picture.Apply();
            RenderTexture.active = previousActive;
            RenderTexture.ReleaseTemporary(target);
            string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", SCREENSHOT_FOLDER));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, VIEW_SCREENSHOT_PREFIX + fileName), picture.EncodeToPNG());
            Object.Destroy(picture);
        }

        // Turns the view through the look input (yaw) and the test pitch until its centre ray points at the drifting piece.
        private static IEnumerator AimViewAtCoroutine(FieldOreId id) {
            IFieldOreQuery query = SessionQueries.Resolve<IFieldOreQuery>();
            float deadline = Time.realtimeSinceStartup + AIM_TIMEOUT_SECONDS;
            while (query.TryGetOre(id, out FieldOreDescription ore) && HandWeaponAiming.KeepAiming(ore.Center) == false) {
                if (Time.realtimeSinceStartup > deadline)
                    Assert.Fail($"Ore: the view did not settle on {id} at {ore.Center}.");

                yield return null;
            }

            yield return null;
        }

        private static Vector3 CurrentCenter(FieldOreId id) {
            SessionQueries.Resolve<IFieldOreQuery>().TryGetOre(id, out FieldOreDescription ore);
            return ore.Center;
        }

        private static bool IsHookedBy(IFieldOreQuery query, FieldOreId id, PlayerKey host) =>
            query.TryGetOre(id, out FieldOreDescription ore) && ore.Hooker.Equals(host);

        private static bool IsShown(FieldOreId id) =>
            SessionQueries.Resolve<FieldOreSceneModel>().TryGetView(id.Value, out FieldOreView view) && view.gameObject.activeInHierarchy;

        private static bool IsRopeShown(FieldOreId id) =>
            SessionQueries.Resolve<FieldOreSceneModel>().TryGetView(id.Value, out FieldOreView view) && view.GetComponentInChildren<LineRenderer>(true).enabled;

        private static bool IsLooseHarpoon(HandWeaponItem item) =>
            item.Definition.Id == HARPOON_ID && item.GetComponent<Grabbable>().CanBeGrabbed;
    }
}
#endif
