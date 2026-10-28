#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Features.BugModule.Scripts.Configuration;
using Features.BugModule.Scripts.View;
using Features.CameraModule.Scripts;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Client;
using Features.FieldAgentModule.Scripts.Network;
using Features.FieldAgentModule.Scripts.Network.Generated;
using Features.GrabModule.Scripts;
using Features.HandWeaponModule.Scripts;
using Mirror;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace Tests.Coop.Client {
    // The client shoots a bug (scenario bugs): in the cruise, on the host's command, with the pistol the host put in
    // its hand, it picks a bug it shows sitting on the ship, turns its view onto it through the look input and clicks
    // the fire button until it no longer shows the bug, capturing the screen. It reports the bug's id and how many of
    // the bugs it shows have their tail rig.
    public sealed class CoopBugsClient : CoopScenarioClientBot {
        private const int LOOP = 1;
        private const string SCREENSHOT = "bug-shot";
        private const int SHOT_ATTEMPTS = 5;
        // Longer than the pistol's cooldown, so every click fires.
        private const float SHOT_SECONDS = 2.2f;
        // The test pitch is not clamped by the camera: past straight up or down the view turns over.
        private const float MAX_PITCH_DEGREES = 85f;
        private const string MESSAGE_FORMAT = "{0} {1}";

        private readonly List<int> _shownIds = new();
        private string _screenshot;

        protected override IEnumerator RunScenarioCoroutine() {
            yield return BoardCoroutine();
            yield return WaitForCruiseCoroutine(LOOP);
            yield return ShootBugCoroutine();
            yield return WaitForLandingCoroutine(LOOP);
        }

        private IEnumerator ShootBugCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_SHOOT_BUG);
            GrabController hand = NetworkClient.localPlayer.GetComponent<GrabController>();
            yield return WaitForCoroutine(() => hand.Held != null && hand.Held.TryGetComponent(out HandWeaponItem _), "the pistol in the hand");
            int bugId = -1;
            yield return WaitForCoroutine(() => TryFindSittingBug(out bugId), "a bug sitting on the ship");
            int tails = CountTailedBugs();
            IFieldAgentShownAgents shown = SessionQueries.Resolve<IFieldAgentShownAgents>();
            CameraLookDriver.DebugPitchOverride = true;
            for (int shot = 0; shot < SHOT_ATTEMPTS && shown.TryGetShownWorldPosition(bugId, out Vector3 _); shot++) {
                for (float aimed = 0f; aimed < SHOT_SECONDS && shown.TryGetShownWorldPosition(bugId, out Vector3 position); aimed += Time.deltaTime) {
                    KeepAiming(position);
                    yield return null;
                }

                if (shown.TryGetShownWorldPosition(bugId, out Vector3 _))
                    yield return HandWeaponAiming.ClickFireCoroutine();
            }

            yield return CaptureScreenCoroutine(SCREENSHOT, path => _screenshot = path);
            CameraLookDriver.DebugPitchOverride = false;
            CameraLookDriver.DebugPitch = 0f;
            HandWeaponAiming.TurnViewBack();
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.BUG_SHOT);
            observation.Message = string.Format(CultureInfo.InvariantCulture, MESSAGE_FORMAT, bugId, tails);
            observation.Screenshot = _screenshot;
            Report(observation);
        }

        // A bug of the last batch in the deck mode that this peer shows.
        private static bool TryFindSittingBug(out int bugId) {
            bugId = -1;
            FieldAgentSnapshot[] agents = SessionQueries.Resolve<IReadOnlyFieldAgentsModel>().Frame.Agents;
            IBugKindService kinds = SessionQueries.Resolve<IBugKindService>();
            IFieldAgentShownAgents shown = SessionQueries.Resolve<IFieldAgentShownAgents>();
            if (agents == null)
                return false;

            foreach (FieldAgentSnapshot agent in agents) {
                if (kinds.IsBug(agent.KindIndex) == false || (FieldAgentMode)agent.Mode != FieldAgentMode.Deck || shown.TryGetShownWorldPosition(agent.Id, out Vector3 _) == false)
                    continue;

                bugId = agent.Id;
                return true;
            }

            return false;
        }

        // Shown bug looks whose tail rig is built and running.
        private int CountTailedBugs() {
            IFieldAgentShownAgents shown = SessionQueries.Resolve<IFieldAgentShownAgents>();
            shown.CollectShownIds(_shownIds);
            int tails = 0;
            foreach (int id in _shownIds) {
                if (shown.TryGetView(id, out FieldAgentView view) && HasRunningTailRig(view))
                    tails++;
            }

            return tails;
        }

        // A bug look whose tail rig is built and evaluating.
        private static bool HasRunningTailRig(FieldAgentView view) {
            if (view.TryGetComponent(out BugBodyView _) == false || view.TryGetComponent(out RigBuilder rig) == false)
                return false;

            return rig.enabled && rig.layers.Count > 0 && rig.layers[0].rig.isActiveAndEnabled;
        }

        private static void KeepAiming(Vector3 point) {
            HandWeaponAiming.KeepAiming(point);
            CameraLookDriver.DebugPitch = Mathf.Clamp(CameraLookDriver.DebugPitch, -MAX_PITCH_DEGREES, MAX_PITCH_DEGREES);
        }
    }
}
#endif
