#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Features.CameraModule.Scripts.Services;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.FlightFieldModule.Scripts.Rocks;
using Features.FlightFieldModule.Scripts.Scene;
using Features.FlightFieldModule.Scripts.Structures;
using Features.FlightFieldModule.Scripts.Targets;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.InputModule.Realization.Scripts.Generated;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Debug;
using Features.TurretModule.Scripts;
using Features.WeaponModule.Contracts;
using Features.WeaponModule.Scripts.Readiness;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // The client bot's weapons, played through the real input: the pistol in the hand (the view turned onto the target
    // through the look input, the fire button clicked) and the turret it sits at (the gun turned by the look input, the
    // fire button held). It shoots the host's debug dummy, the field rock or balloon the host named, and reports what its
    // own peer saw: the shot, the broken rock or popped balloon gone from its view.
    public sealed class CoopClientWeapons {
        private const int FIELD_ROCK_LAYER_MASK = 1 << 8;
        private const float READY = 1f;
        // Longer than the pistol's cooldown, so a click then fires.
        private const float PISTOL_AIM_SECONDS = 2.2f;
        // A second click this soon after a shot falls inside the 2 s cooldown.
        private const float BLOCKED_CLICK_SECONDS = 0.5f;
        private const float READINESS_TOLERANCE = 0.01f;
        private const float COOLDOWN_TIMEOUT_SECONDS = 3f;
        private const int PISTOL_ATTEMPTS = 3;
        private const float PISTOL_RESULT_SECONDS = 1.5f;
        private const float TARGET_FIRE_TIMEOUT_SECONDS = 12f;
        private const float SCREENSHOT_AFTER_SECONDS = 1f;
        private const float SEAT_DROP_HEIGHT = 0.5f;
        private const float SEAT_SETTLE_SECONDS = 1f;
        private const float CROSSHAIR_DESCRIBED_METERS = 300f;
        private const float TARGET_INNER_SHARE = 0.4f;
        private const float PISTOL_DROP_FROM_SEAT_METERS = 2f;
        // Look input per degree of turret aim error: under 1 / LookSensitivity (0.12 degrees per unit), so it never overshoots.
        private const float TURRET_LOOK_PER_DEGREE = 4f;
        private const float MAX_TURRET_LOOK = 60f;
        private const string PISTOL_SCREENSHOT = "pistol-shot";
        private const string TURRET_SCREENSHOT_FORMAT = "turret-{0}";

        private readonly CoopTestClient _client;
        private readonly CoopClientHands _hands;
        private readonly CoopClientCommands _commands;
        private readonly CoopClientScreenshots _screenshots;
        private readonly WaitForSeconds _seatSettle = new WaitForSeconds(SEAT_SETTLE_SECONDS);
        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private bool _isTargetBlocked;

        private static ShipBase Ship => Object.FindAnyObjectByType<ShipBase>();
        private static IInputService Input => SessionQueries.Resolve<IInputService>();
        private static float Readiness => SessionQueries.Resolve<HeldWeaponReadinessModel>().Readiness;
        private static IReadOnlyLiveFieldModel Field => SessionQueries.Resolve<IReadOnlyLiveFieldModel>();

        public CoopClientWeapons(CoopTestClient client, CoopClientHands hands, CoopClientCommands commands, CoopClientScreenshots screenshots) {
            _client = client;
            _hands = hands;
            _commands = commands;
            _screenshots = screenshots;
        }

        // A click fires; a second click within the cooldown fires nothing (the ring keeps filling); once the ring is full
        // a click fires again. The host counts what arrived.
        public IEnumerator FirePistolAtDummyCoroutine() {
            yield return _client.WaitForCoroutine(IsPistolInHand, "the pistol in the hand");
            WeaponDebugTarget target = null;
            yield return _client.WaitForCoroutine(() => (target = Object.FindAnyObjectByType<WeaponDebugTarget>()) != null, "the host's debug dummy");
            Func<Vector3> dummy = () => target.GetComponentInChildren<Collider>().bounds.center;
            yield return AimPistolCoroutine(dummy, PISTOL_AIM_SECONDS);
            yield return ClickPistolCoroutine(true);
            yield return _screenshots.CaptureCoroutine(PISTOL_SCREENSHOT);
            yield return AimPistolCoroutine(dummy, BLOCKED_CLICK_SECONDS);
            float readinessBefore = Readiness;
            yield return ClickPistolCoroutine(false);
            if (Readiness < readinessBefore - READINESS_TOLERANCE)
                yield return _client.FailCoroutine($"A click {BLOCKED_CLICK_SECONDS} s after a shot fired again: readiness {readinessBefore:0.00} -> {Readiness:0.00}.");

            yield return AimPistolUntilReadyCoroutine(dummy, 0f);
            yield return ClickPistolCoroutine(true);
            HandWeaponAiming.TurnViewBack();
            Report(CoopWeaponsEconomySteps.PISTOL_FIRED, "2 shots, 1 click blocked by the cooldown");
        }

        // A step from the turret seat toward the deck's middle, so the gunner finds it near when it stands up in the
        // cruise (under the seat's volume the crosshair would find the seat, not the pistol).
        public IEnumerator DropPistolCoroutine() {
            ShipSocket socket = FindTurretSocket();
            Vector3 sitSpot = Ship.transform.InverseTransformPoint(socket.Seat.SitPoint.position);
            Vector3 inward = new Vector3(-sitSpot.x, 0f, -sitSpot.z).normalized;
            ShipDeckBoarding.PutAbove(CoopClientHands.Body, Ship, sitSpot + inward * PISTOL_DROP_FROM_SEAT_METERS, SEAT_DROP_HEIGHT);
            yield return _seatSettle;
            yield return _hands.DropCoroutine();
            Report(CoopWeaponsEconomySteps.PISTOL_DROPPED, string.Empty);
        }

        public IEnumerator PickUpPistolCoroutine() {
            HandWeaponItem pistol = Object.FindAnyObjectByType<HandWeaponItem>();
            if (pistol == null)
                yield return _client.FailCoroutine("No pistol in the client's scene to pick up.");

            Grabbable item = pistol.GetComponent<Grabbable>();
            yield return _hands.AimAtVisiblePartCoroutine(item, "the dropped pistol");
            if (CoopClientHands.Interaction.Target.Grabbable != item) {
                yield return _hands.WalkToCoroutine(() => item.transform.position, Ship, "the dropped pistol");
                yield return _hands.AimAtVisiblePartCoroutine(item, "the dropped pistol");
            }

            if (CoopClientHands.Interaction.Target.Grabbable != item)
                yield return _client.FailCoroutine($"The dropped pistol is not under the crosshair ({CoopClientHands.DescribeView()}).");

            CoopClientHands.Interaction.Interact();
            yield return _client.WaitForCoroutine(IsPistolInHand, "the dropped pistol back in the hand");
            Report(CoopWeaponsEconomySteps.PISTOL_PICKED_UP, string.Empty);
        }

        // Once this peer shows the gun the host installed: beside the seat, then the seat's use command (what E sends),
        // as the coop helm step sits down.
        public IEnumerator SitAtTurretCoroutine(string moduleId) {
            ShipSocket socket = FindTurretSocket();
            yield return _client.WaitForCoroutine(() => socket.InstalledModuleId == moduleId && FindTurretStation() != null, $"{moduleId} shown in the turret socket");
            Vector3 sitSpot = Ship.transform.InverseTransformPoint(socket.Seat.SitPoint.position);
            ShipDeckBoarding.PutAbove(CoopClientHands.Body, Ship, sitSpot, SEAT_DROP_HEIGHT);
            yield return _seatSettle;
            SessionQueries.LocalPlayer.GetComponent<UseController>().RequestUse(socket.GetComponentInChildren<ShipSitInteractable>());
            yield return _client.WaitForCoroutine(() => FindTurretStation().IsOccupiedByLocalPlayer, $"the client seated at {moduleId}");
            Report(CoopWeaponsEconomySteps.SEATED_AT_TURRET, socket.InstalledModuleId);
        }

        public IEnumerator StandUpCoroutine() {
            ShipSocket socket = FindTurretSocket();
            SessionQueries.LocalPlayer.GetComponent<UseController>().RequestUse(socket.GetComponentInChildren<ShipSitInteractable>());
            yield return _client.WaitForCoroutine(() => FindTurretStation().IsOccupiedByLocalPlayer == false, "the client stood up from the turret");
            yield return _seatSettle;
            Report(CoopWeaponsEconomySteps.STOOD_UP, string.Empty);
        }

        // Holds the trigger on the swaying dummy, the gun following it, until the host says stop.
        public IEnumerator FireTurretAtDummyCoroutine() {
            WeaponDebugTarget target = null;
            yield return _client.WaitForCoroutine(() => (target = Object.FindAnyObjectByType<WeaponDebugTarget>()) != null, "the host's debug dummy");
            string moduleId = FindTurretSocket().InstalledModuleId;
            yield return TurnTurretOntoCoroutine(() => target.GetComponentInChildren<Collider>().bounds.center);
            Input.Fire.Started?.Invoke();
            Report(CoopWeaponsEconomySteps.TURRET_FIRING, moduleId);
            float screenshotAt = Time.realtimeSinceStartup + SCREENSHOT_AFTER_SECONDS;
            bool hasScreenshot = false;
            while (_commands.TryTakeNextPolled() == false) {
                KeepTurretAiming(target.GetComponentInChildren<Collider>().bounds.center);
                if (hasScreenshot == false && Time.realtimeSinceStartup > screenshotAt) {
                    hasScreenshot = true;
                    yield return _screenshots.CaptureCoroutine(string.Format(TURRET_SCREENSHOT_FORMAT, moduleId));
                }

                yield return null;
            }

            Input.Fire.Canceled?.Invoke();
            if (_commands.Name != CoopWeaponsEconomySteps.COMMAND_STOP_FIRE)
                yield return _client.FailCoroutine($"The host sent '{_commands.Name}' while the client fired at the dummy.");

            Report(CoopWeaponsEconomySteps.TURRET_FIRED, moduleId);
        }

        public IEnumerator TurretShootRockCoroutine(FieldRockId rockId) {
            if (SessionQueries.Resolve<IPeerFieldRockQuery>().TryGetRock(rockId, out FieldRockDescription rock) == false)
                yield return _client.FailCoroutine($"The client's field has no unbroken rock {rockId}.");

            yield return TurretFireUntilCoroutine(() => rock.Center, () => Contains(Field.BrokenRocks, rockId), $"rock {rockId}");
            yield return null;
            Report(CoopWeaponsEconomySteps.ROCK_BROKEN, IsRockShown(rockId) ? "still shown" : "hidden");
        }

        public IEnumerator TurretShootBalloonCoroutine(FieldTargetId balloonId) {
            if (SessionQueries.Resolve<IPeerFieldTargetQuery>().TryGetTarget(balloonId, out FieldTargetDescription balloon) == false)
                yield return _client.FailCoroutine($"The client's field has no unpopped balloon {balloonId}.");

            yield return TurretFireUntilCoroutine(() => ShownBalloonCenter(balloon), () => Contains(Field.PoppedTargets, balloonId), $"balloon {balloonId}");
            yield return null;
            Report(CoopWeaponsEconomySteps.BALLOON_POPPED, IsBalloonShown(balloonId) ? "still shown" : "hidden");
        }

        public IEnumerator PistolShootRockCoroutine(FieldRockId rockId) {
            if (SessionQueries.Resolve<IPeerFieldRockQuery>().TryGetRock(rockId, out FieldRockDescription rock) == false)
                yield return _client.FailCoroutine($"The client's field has no unbroken rock {rockId}.");

            ulong targetId = SessionQueries.Resolve<IFieldRockTargetIds>().GetTargetId(rockId);
            yield return PistolShootCoroutine(() => rock.Center, 0f, targetId, () => Contains(Field.BrokenRocks, rockId), $"rock {rockId}");
            if (_isTargetBlocked)
                yield break;

            Report(CoopWeaponsEconomySteps.ROCK_BROKEN, IsRockShown(rockId) ? "still shown" : "hidden");
        }

        public IEnumerator PistolShootBalloonCoroutine(FieldTargetId balloonId) {
            Debug.Log($"[CoopClient] balloon {balloonId} shown {IsBalloonShown(balloonId)}");
            if (SessionQueries.Resolve<IPeerFieldTargetQuery>().TryGetTarget(balloonId, out FieldTargetDescription balloon) == false)
                yield return _client.FailCoroutine($"The client's field has no unpopped balloon {balloonId}.");

            ulong targetId = SessionQueries.Resolve<IFieldTargetWeaponIds>().GetTargetId(balloonId);
            yield return PistolShootCoroutine(() => ShownBalloonCenter(balloon), balloon.Radius, targetId, () => Contains(Field.PoppedTargets, balloonId), $"balloon {balloonId}");
            if (_isTargetBlocked)
                yield break;

            Report(CoopWeaponsEconomySteps.BALLOON_POPPED, IsBalloonShown(balloonId) ? "still shown" : "hidden");
        }

        // A balloon the host popped (the ship flew into it): gone from this peer's view too.
        public IEnumerator SeeBalloonPoppedCoroutine(FieldTargetId balloonId) {
            yield return _client.WaitForCoroutine(() => Contains(Field.PoppedTargets, balloonId) && IsBalloonShown(balloonId) == false, $"balloon {balloonId} popped and gone on the client");
            Report(CoopWeaponsEconomySteps.BALLOON_POPPED, "hidden");
        }

        // The view onto the target, the first thing on its ray must be the target, then click until it is hit, a
        // cooldown apart. A target behind something is reported to the host, which picks another one.
        private IEnumerator PistolShootCoroutine(Func<Vector3> center, float radius, ulong targetId, Func<bool> isDone, string what) {
            _isTargetBlocked = false;
            yield return _client.WaitForCoroutine(IsPistolInHand, "the pistol in the hand");
            yield return _hands.AimAtCoroutine(center, what);
            if (IsFirstStruck(center(), targetId) == false) {
                Debug.Log($"[CoopClient] {what} is not in sight: crosshair on {DescribeCrosshair()}");
                _isTargetBlocked = true;
                HandWeaponAiming.TurnViewBack();
                Report(CoopWeaponsEconomySteps.TARGET_BLOCKED, what);
                yield break;
            }

            StringBuilder shots = new StringBuilder();
            for (int attempt = 0; attempt < PISTOL_ATTEMPTS && isDone() == false; attempt++) {
                yield return AimPistolUntilReadyCoroutine(center, radius);
                shots.Append($"[shot {attempt + 1}: target at {center()}, crosshair on {DescribeCrosshair()}, ship at {Ship.transform.position}] ");
                yield return ClickPistolCoroutine(true);
                float until = Time.realtimeSinceStartup + PISTOL_RESULT_SECONDS;
                while (isDone() == false && Time.realtimeSinceStartup < until)
                    yield return null;
            }

            HandWeaponAiming.TurnViewBack();
            if (isDone())
                yield break;

            // Missed every time: logged, and the host picks another target as for one out of sight.
            string missed = $"{PISTOL_ATTEMPTS} pistol shots did not finish {what}: {shots}{CoopClientHands.DescribeView()}";
            Debug.LogWarning($"[CoopClient] {missed}");
            _isTargetBlocked = true;
            Report(CoopWeaponsEconomySteps.TARGET_BLOCKED, missed);
        }

        private IEnumerator TurretFireUntilCoroutine(Func<Vector3> center, Func<bool> isDone, string what) {
            yield return TurnTurretOntoCoroutine(center);
            Input.Fire.Started?.Invoke();
            float deadline = Time.realtimeSinceStartup + TARGET_FIRE_TIMEOUT_SECONDS;
            while (isDone() == false) {
                if (Time.realtimeSinceStartup > deadline) {
                    Input.Fire.Canceled?.Invoke();
                    yield return _client.FailCoroutine($"The turret's fire did not finish {what} within {TARGET_FIRE_TIMEOUT_SECONDS} s.");
                }

                KeepTurretAiming(center());
                yield return null;
            }

            Input.Fire.Canceled?.Invoke();
        }

        // The trigger goes down once the gun is on the target or after a while: a bobbing balloon never holds still for
        // the exact aim, and the gun keeps following it while it fires; what it hits decides.
        private static IEnumerator TurnTurretOntoCoroutine(Func<Vector3> center) {
            float deadline = Time.realtimeSinceStartup + COOLDOWN_TIMEOUT_SECONDS;
            while (KeepTurretAiming(center()) == false && Time.realtimeSinceStartup < deadline)
                yield return null;
        }

        // One step of the mouse on the seated turret: the camera rides the gun, so its error is the gun's.
        private static bool KeepTurretAiming(Vector3 target) {
            Transform view = SessionQueries.Resolve<IGameCameraService>().OutputCamera.transform;
            Vector3 toTarget = target - view.position;
            float yawError = Vector3.SignedAngle(Vector3.ProjectOnPlane(view.forward, Vector3.up), Vector3.ProjectOnPlane(toTarget, Vector3.up), Vector3.up);
            float pitchError = Elevation(toTarget) - Elevation(view.forward);
            if (Mathf.Abs(yawError) < HandWeaponAiming.AIM_TOLERANCE_DEGREES && Mathf.Abs(pitchError) < HandWeaponAiming.AIM_TOLERANCE_DEGREES)
                return true;

            Vector2 look = new Vector2(
                Mathf.Clamp(yawError * TURRET_LOOK_PER_DEGREE, -MAX_TURRET_LOOK, MAX_TURRET_LOOK),
                Mathf.Clamp(pitchError * TURRET_LOOK_PER_DEGREE, -MAX_TURRET_LOOK, MAX_TURRET_LOOK));
            Input.Look.VectorChangedPerformed?.Invoke(look);
            return false;
        }

        private static float Elevation(Vector3 direction) =>
            Mathf.Asin(Mathf.Clamp(direction.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;

        private static IEnumerator AimPistolCoroutine(Func<Vector3> target, float seconds) {
            for (float aimed = 0f; aimed < seconds; aimed += Time.deltaTime) {
                HandWeaponAiming.KeepAiming(target());
                yield return null;
            }
        }

        // radius: a target this big may be hit off its centre, so the view only has to be on its inner part (a bobbing
        // balloon never holds still for the exact aim).
        private IEnumerator AimPistolUntilReadyCoroutine(Func<Vector3> target, float radius) {
            float deadline = Time.realtimeSinceStartup + COOLDOWN_TIMEOUT_SECONDS;
            while (Readiness < READY || IsOnTarget(target(), radius) == false) {
                if (Time.realtimeSinceStartup > deadline)
                    yield return _client.FailCoroutine($"The pistol was not ready and on target within {COOLDOWN_TIMEOUT_SECONDS} s (readiness {Readiness:0.00}).");

                yield return null;
            }

            yield return null;
        }

        private static bool IsOnTarget(Vector3 target, float radius) {
            Transform view = SessionQueries.Resolve<IGameCameraService>().OutputCamera.transform;
            Vector3 toTarget = target - view.position;
            float tolerance = Mathf.Max(HandWeaponAiming.AIM_TOLERANCE_DEGREES, Mathf.Atan2(radius * TARGET_INNER_SHARE, toTarget.magnitude) * Mathf.Rad2Deg);
            bool isAimed = HandWeaponAiming.KeepAiming(target);
            return isAimed || Vector3.Angle(view.forward, toTarget) < tolerance;
        }

        private IEnumerator ClickPistolCoroutine(bool isReady) {
            yield return HandWeaponAiming.ClickFireCoroutine();
            if (isReady && Readiness >= READY)
                yield return _client.FailCoroutine($"A click on the ready pistol did not fire (cursor {Cursor.lockState}).");
        }

        private string DescribeCrosshair() {
            Transform view = SessionQueries.Resolve<IGameCameraService>().OutputCamera.transform;
            if (Physics.Raycast(view.position, view.forward, out RaycastHit hit, CROSSHAIR_DESCRIBED_METERS, FIELD_ROCK_LAYER_MASK, QueryTriggerInteraction.Collide) == false)
                return "nothing";

            ulong targetId = hit.collider.TryGetComponent(out IWeaponTargetCollider collider) ? collider.TargetId : 0UL;
            return $"{hit.collider.name} target {targetId:X} at {hit.distance:0.0} m";
        }

        private bool IsFirstStruck(Vector3 center, ulong targetId) {
            Transform view = SessionQueries.Resolve<IGameCameraService>().OutputCamera.transform;
            Vector3 toTarget = center - view.position;
            int count = Physics.RaycastNonAlloc(view.position, toTarget.normalized, _hits, toTarget.magnitude, FIELD_ROCK_LAYER_MASK, QueryTriggerInteraction.Collide);
            float nearest = float.MaxValue;
            bool isTargetNearest = false;
            for (int i = 0; i < count; i++) {
                bool hasTarget = _hits[i].collider.TryGetComponent(out IWeaponTargetCollider collider);
                if ((_hits[i].collider.isTrigger && hasTarget == false) || _hits[i].distance >= nearest)
                    continue;

                nearest = _hits[i].distance;
                isTargetNearest = hasTarget && collider.TargetId == targetId;
            }

            return isTargetNearest;
        }

        private static bool IsPistolInHand() {
            Grabbable held = CoopClientHands.Hand.Held;
            return held != null && held.TryGetComponent(out HandWeaponItem _);
        }

        private static bool IsRockShown(FieldRockId id) {
            FieldRockVisual rock;
            bool isBuilt;
            if (id.Source == FieldRockSource.Cluster)
                isBuilt = SessionQueries.Resolve<IFieldStructureSceneBuilder>().TryGetBuiltRock(id.PlacementIndex, id.RockIndex, out rock);
            else
                isBuilt = SessionQueries.Resolve<IFieldDebrisSceneBuilder>().TryGetBuiltRock(id.DebrisCell, id.RockIndex, out rock);

            return isBuilt && rock.gameObject.activeInHierarchy;
        }

        // A balloon bobs in its view: aimed at as shown, not at its centre in the data; the data is read again each time,
        // as a world shift moves the field.
        private static Vector3 ShownBalloonCenter(FieldTargetDescription balloon) {
            if (SessionQueries.Resolve<IReadOnlyFieldTargetSceneModel>().TryGetView(balloon.Id.Index, out FieldTargetView view))
                return view.GetComponentInChildren<Collider>().bounds.center;

            return SessionQueries.Resolve<IPeerFieldTargetQuery>().TryGetTarget(balloon.Id, out FieldTargetDescription current) ? current.Center : balloon.Center;
        }

        private static bool IsBalloonShown(FieldTargetId id) =>
            SessionQueries.Resolve<IReadOnlyFieldTargetSceneModel>().TryGetView(id.Index, out FieldTargetView _);

        private static bool Contains<TId>(IReadOnlyList<TId> ids, TId id) where TId : IEquatable<TId> {
            for (int i = 0; i < ids.Count; i++) {
                if (ids[i].Equals(id))
                    return true;
            }

            return false;
        }

        // The slot view of the installed gun, found again each time: a swapped gun brings a new one.
        private static ShipTurretStation FindTurretStation() =>
            FindTurretSocket().GetComponentInChildren<ShipTurretStation>();

        private static ShipSocket FindTurretSocket() {
            foreach (ShipSocket socket in Ship.Sockets) {
                if (socket.SocketId == CoopWeaponsEconomySteps.TURRET_SOCKET)
                    return socket;
            }

            throw new InvalidOperationException($"The ship has no socket {CoopWeaponsEconomySteps.TURRET_SOCKET}.");
        }

        private void Report(string step, string message) {
            CoopObservation observation = CoopWeaponsEconomyObserver.Capture(step);
            observation.Message = message;
            _client.Report(observation);
        }
    }
}
#endif
