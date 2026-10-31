#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Features.CharacterMovableModule.Scripts.Models;
using Features.CutsceneModule.Scripts.Models;
using Features.LobbyModule.Scripts;
using Features.MvpModule;
using Features.PointOfInterestModule.Scripts.Client;
using Features.PointOfInterestModule.Scripts.Network;
using Features.SonarModule.Scripts.Core;
using Mirror;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;

namespace Tests.Coop.Client {
    // The client plays a bug nest with the host (scenario point-of-interest): it reports the field's points as it has
    // them (id, position, a look shown, a sonar contact), plays the entry cutscene the host's ship starts (its start
    // and end in network time, its controls blocked and HUD hidden meanwhile) and reports the payout it sees.
    public sealed class CoopPointOfInterestClient : CoopScenarioClientBot {
        private const int LOOP = 1;
        private const string POINTS_SCREENSHOT = "points-seen";
        private const string CUTSCENE_SCREENSHOT = "cutscene";
        private const string CONTACT_PREFIX = "poi:";
        private const float CUTSCENE_SCREENSHOT_SECONDS = 1f;
        private const char POINT_SEPARATOR = ';';
        private const string POINT_FORMAT = "{0}:{1:R},{2:R},{3:R},{4},{5}";
        private const string CUTSCENE_FORMAT = "{0:R} {1:R} {2} {3}";

        private string _screenshot;

        protected override IEnumerator RunScenarioCoroutine() {
            yield return BoardCoroutine();
            yield return WaitForCruiseCoroutine(LOOP);
            yield return ReportPointsCoroutine();
            yield return WatchCutsceneCoroutine();
            yield return ReportPayoutCoroutine();
            yield return WaitForLandingCoroutine(LOOP);
        }

        private IEnumerator ReportPointsCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_REPORT_POINTS);
            IPointsOfInterest points = SessionQueries.Resolve<IPointsOfInterest>();
            yield return WaitForCoroutine(() => points.Count > 0 && AreAllShown(points), "the points of interest and their looks");
            yield return CaptureScreenCoroutine(POINTS_SCREENSHOT, path => _screenshot = path);
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.POINTS_SEEN);
            observation.Message = DescribePoints(points);
            observation.Screenshot = _screenshot;
            Report(observation);
        }

        private IEnumerator WatchCutsceneCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_WATCH_CUTSCENE);
            Report(CoopFlightObserver.CaptureFlight(CoopFlightSteps.WATCHING));
            CutsceneLocalModel cutscene = SessionQueries.Resolve<CutsceneLocalModel>();
            yield return WaitForCoroutine(() => cutscene.IsPlaying, "the entry cutscene");
            double start = NetworkTime.time;
            bool isBlocked = SessionQueries.Resolve<PlayerControlBlockModel>().IsBlocked;
            bool isHudHidden = SessionQueries.Resolve<IWindowsService>().GetWindow(typeof(GameHudWindow)).WindowStatus == WindowStatus.Hidden;
            yield return new WaitForSeconds(CUTSCENE_SCREENSHOT_SECONDS);
            yield return CaptureScreenCoroutine(CUTSCENE_SCREENSHOT, path => _screenshot = path);
            yield return WaitForCoroutine(() => !cutscene.IsPlaying, "the end of the entry cutscene");
            CoopObservation observation = CoopFlightObserver.CaptureFlight(CoopFlightSteps.CUTSCENE_WATCHED);
            observation.Message = string.Format(CultureInfo.InvariantCulture, CUTSCENE_FORMAT, start, NetworkTime.time, isBlocked, isHudHidden);
            observation.Screenshot = _screenshot;
            Report(observation);
        }

        private IEnumerator ReportPayoutCoroutine() {
            yield return WaitForCommandCoroutine(CoopFlightSteps.COMMAND_REPORT_PAYOUT);
            IPointsOfInterest points = SessionQueries.Resolve<IPointsOfInterest>();
            yield return WaitForCoroutine(() => IsAnyOpenedAndGone(points), "an opened container gone from the client's view");
            Report(CoopFlightObserver.CaptureFlight(CoopFlightSteps.PAYOUT_SEEN));
        }

        private static bool AreAllShown(IPointsOfInterest points) {
            PointOfInterestSceneModel scene = SessionQueries.Resolve<PointOfInterestSceneModel>();
            for (int i = 0; i < points.Count; i++) {
                if (!scene.TryGetView(points.Get(i).Id, out PointOfInterestView _))
                    return false;
            }

            return true;
        }

        private static bool IsAnyOpenedAndGone(IPointsOfInterest points) {
            PointOfInterestSceneModel scene = SessionQueries.Resolve<PointOfInterestSceneModel>();
            for (int i = 0; i < points.Count; i++) {
                PointOfInterestEntry point = points.Get(i);
                if (point.State == PointOfInterestState.Opened && !scene.TryGetView(point.Id, out PointOfInterestView _))
                    return true;
            }

            return false;
        }

        // "id:x,y,z,shown,on sonar" per point, separated by ';'.
        private static string DescribePoints(IPointsOfInterest points) {
            PointOfInterestSceneModel scene = SessionQueries.Resolve<PointOfInterestSceneModel>();
            ISonarContactService sonar = SessionQueries.Resolve<ISonarContactService>();
            List<string> described = new();
            for (int i = 0; i < points.Count; i++) {
                PointOfInterestEntry point = points.Get(i);
                bool isShown = scene.TryGetView(point.Id, out PointOfInterestView _);
                bool isOnSonar = sonar.TryGetContact(CONTACT_PREFIX + point.Id.ToString(CultureInfo.InvariantCulture), out _);
                described.Add(string.Format(CultureInfo.InvariantCulture, POINT_FORMAT, point.Id, point.Position.x, point.Position.y, point.Position.z, isShown, isOnSonar));
            }

            return string.Join(POINT_SEPARATOR.ToString(), described);
        }
    }
}
#endif
