#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using Features.CameraModule.Scripts;
using Features.FlightFieldModule.Contracts;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Features.WeaponModule.Scripts.Readiness;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;

namespace Tests.Coop.Client {
    // The client earns money with the harpoon (scenario harpoon): in the first cruise it reports the ore it sees from
    // the rock the host broke; with the harpoon the host put in its hand it aims the view at the nearest piece through
    // the look input and clicks the fire button until its own rope hooks it, and reports the hook and its rope; after
    // the delivery it reports the piece gone and the crew balance. Each step on the host's command.
    public sealed class CoopHarpoonClient : CoopScenarioClientBot {
        private const int LOOP = 1;
        private const int THROW_ATTEMPTS = 3;
        private const float AIM_SECONDS = 1f;
        private const float HOOK_WAIT_SECONDS = 1.5f;
        private const float RELOAD_WAIT_SECONDS = 2f;
        private const float READY = 1f;
        private const string ORE_SCREENSHOT = "ore-seen";
        private const string ROPE_SCREENSHOT = "harpoon-rope";

        private readonly List<FieldOreDescription> _ores = new List<FieldOreDescription>();
        private int _hookedOreId;

        private static GrabController Hand => SessionQueries.LocalPlayer.GetComponent<GrabController>();

        protected override IEnumerator RunScenarioCoroutine() {
            yield return BoardCoroutine();
            yield return WaitForCruiseCoroutine(LOOP);
            yield return ReportOreCoroutine();
            yield return ThrowHarpoonCoroutine();
            yield return ReportDeliveryCoroutine();
        }

        private IEnumerator ReportOreCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_REPORT_ORE);
            yield return WaitForCoroutine(IsEveryPieceShown, "the host's ore shown on this peer");
            string screenshot = null;
            yield return CaptureScreenCoroutine(ORE_SCREENSHOT, path => screenshot = path);
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.ORE_SEEN);
            observation.Screenshot = screenshot;
            Report(observation);
        }

        private IEnumerator ThrowHarpoonCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_THROW_HARPOON);
            yield return WaitForCoroutine(() => Hand.Held != null && Hand.Held.TryGetComponent(out HandWeaponItem _), "the harpoon in the hand");
            HeldWeaponReadinessModel readiness = SessionQueries.Resolve<HeldWeaponReadinessModel>();
            try {
                CameraLookDriver.DebugPitchOverride = true;
                for (int attempt = 1; _hookedOreId == 0; attempt++) {
                    if (attempt > THROW_ATTEMPTS)
                        yield return FailCoroutine($"None of {THROW_ATTEMPTS} ropes hooked a piece of ore.");

                    yield return AimAtNearestPieceCoroutine();
                    yield return HandWeaponAiming.ClickFireCoroutine();
                    if (readiness.Readiness >= READY)
                        yield return FailCoroutine($"A click on the ready harpoon threw no rope (cursor {Cursor.lockState}).");

                    for (float waited = 0f; waited < HOOK_WAIT_SECONDS && _hookedOreId == 0; waited += Time.deltaTime) {
                        _hookedOreId = FindOwnHookedPiece();
                        yield return null;
                    }

                    if (_hookedOreId == 0)
                        yield return new WaitForSeconds(RELOAD_WAIT_SECONDS);
                }
            } finally {
                CameraLookDriver.DebugPitchOverride = false;
                CameraLookDriver.DebugPitch = 0f;
            }

            string screenshot = null;
            yield return CaptureScreenCoroutine(ROPE_SCREENSHOT, path => screenshot = path);
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.HARPOON_HOOKED);
            observation.Screenshot = screenshot;
            Report(observation);
            HandWeaponAiming.TurnViewBack();
        }

        private IEnumerator ReportDeliveryCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_REPORT_DELIVERY);
            IFieldOreQuery query = SessionQueries.Resolve<IFieldOreQuery>();
            yield return WaitForCoroutine(() => query.TryGetOre(new FieldOreId(_hookedOreId), out _) == false, $"ore {_hookedOreId} gone at the ship");
            yield return Settle;
            Report(CoopFlightObserver.CaptureFlight(CoopFlightSteps.ORE_DELIVERED));
        }

        // Turns the view through the look input and the test pitch onto the nearest piece as it drifts.
        private IEnumerator AimAtNearestPieceCoroutine() {
            for (float aimed = 0f; aimed < AIM_SECONDS; aimed += Time.deltaTime) {
                if (TryFindNearestPiece(out FieldOreDescription piece))
                    HandWeaponAiming.KeepAiming(piece.Center);

                yield return null;
            }
        }

        private bool TryFindNearestPiece(out FieldOreDescription nearest) {
            nearest = default;
            SessionQueries.Resolve<IFieldOreQuery>().FindAll(_ores);
            Vector3 from = SessionQueries.LocalPlayer.transform.position;
            float nearestDistance = float.MaxValue;
            foreach (FieldOreDescription ore in _ores) {
                float distance = (ore.Center - from).sqrMagnitude;
                if (ore.IsHooked || distance >= nearestDistance)
                    continue;

                nearest = ore;
                nearestDistance = distance;
            }

            return nearestDistance < float.MaxValue;
        }

        private int FindOwnHookedPiece() {
            SessionQueries.Resolve<IFieldOreQuery>().FindAll(_ores);
            foreach (FieldOreDescription ore in _ores) {
                if (ore.Hooker.Equals(Hand.Key))
                    return ore.Id.Value;
            }

            return 0;
        }

        private bool IsEveryPieceShown() {
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.ORE_SEEN);
            if (observation.OreIds.Length == 0)
                return false;

            foreach (bool isShown in observation.OreShown) {
                if (isShown == false)
                    return false;
            }

            return true;
        }
    }
}
#endif
