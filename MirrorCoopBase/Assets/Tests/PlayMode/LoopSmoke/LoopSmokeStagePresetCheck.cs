#if UNITY_INCLUDE_TESTS
using System.Linq;
using Features.FlightFieldModule.Scripts.Configuration;
using Features.FlightFieldModule.Scripts.Model;
using Features.FlightFieldModule.Scripts.Server;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Generated;
using Features.StagePresetModule.Data;
using Features.StagePresetModule.Data.Generated;
using Features.StagePresetModule.Scripts.Configurations;
using Features.StagePresetModule.Scripts.Core;
using Features.StagePresetModule.Scripts.UI;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // A playtest run with the Late stage preset: the host picks it in the lobby picker, the run starts on its loop with
    // its modules and money, the first station drops nothing, the first field is one of the loop's, and back in the
    // lobby the preset is gone.
    public sealed class LoopSmokeStagePresetCheck {
        private const string LATE_PRESET_ID = "late";
        private const string LATE_LABEL = "Preset: Late";

        private readonly LoopSmokeSession _session;
        private readonly StagePreset _late;
        private int _stationItems;

        public LoopSmokeStagePresetCheck(LoopSmokeContext context) {
            _session = context.Session;
            Assert.IsTrue(SessionQueries.Resolve<IStagePresetSelectionService>().TryGetPreset(LATE_PRESET_ID, out _late), $"No stage preset '{LATE_PRESET_ID}'.");
        }

        // The host's click on the Late option of the lobby picker.
        [LoopSmokeStep(LoopSmokePhase.InLobby, 3, 10)]
        private void PickLateInLobby() {
            StagePresetOptionButton late = Object.FindObjectsByType<StagePresetOptionButton>(FindObjectsSortMode.None)
                .FirstOrDefault(option => option.GetComponentInChildren<Text>().text == _late.DisplayName);
            Assert.IsNotNull(late, "The lobby has no visible Late option in the stage preset picker.");
            late.GetComponent<Button>().onClick.Invoke();

            Assert.AreEqual(LATE_PRESET_ID, SessionQueries.Resolve<IReadOnlyRunStagePresetModel>().PresetId, "The synced preset is not Late after the pick.");
            AssertLabelShowsLate("lobby");
        }

        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, 3, 1, 0)]
        private void CountStartStationItems() =>
            _stationItems = LoopSmokeSpawnCounts.CountSceneObjects<ShipItem>();

        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, 3, 1, 10)]
        private void AssertRunStartsAsLate() {
            Assert.AreEqual(_late.StartLoopIndex, _session.Model.LoopIndex, "Late: start loop.");
            Assert.AreEqual(_late.StartingBalance, SessionQueries.Resolve<IReadOnlyWalletModel>().Balance, "Late: starting balance.");
            Assert.IsTrue(_session.Ship.CanLaunch, "Late: the ship cannot launch with the preset's engines.");
            AssertModulesInstalled();
            AssertLabelShowsLate("game");
        }

        private void AssertModulesInstalled() {
            foreach (StagePresetSocketModule entry in _late.Modules) {
                ShipSocket socket = _session.Ship.Sockets.First(candidate => candidate.SocketId == entry.SocketId);
                Assert.AreEqual(entry.Module.Id, socket.InstalledModuleId, $"Late: socket {entry.SocketId}.");
            }
        }

        // The field of the first flight comes from the loop table entry of the preset's start loop.
        [LoopSmokeStep(LoopSmokePhase.InCruise, 3, 1, 10)]
        private void AssertFieldOfStartLoop() {
            FlightField field = SessionQueries.Resolve<IReadOnlyFlightFieldServerModel>().Field;
            FlightFieldConfiguration.LoopRoutes loop = SessionQueries.Resolve<FlightFieldConfiguration>().RoutesByLoop
                .Where(entry => entry.FromLoop <= _late.StartLoopIndex)
                .OrderBy(entry => entry.FromLoop)
                .Last();
            Assert.IsTrue(loop.Profiles.Any(option => option.Profile.Id == field.ProfileId), $"Late: field profile {field.ProfileId} is not offered on loop {_late.StartLoopIndex}.");
            Assert.AreEqual(loop.RouteDifficulties.Max(), field.Difficulty, 0.0001f, $"Late: field difficulty is not the one of loop {_late.StartLoopIndex}.");
        }

        [LoopSmokeStep(LoopSmokePhase.AfterLanding, 3, 1, 10)]
        private void AssertFirstStationDroppedNothing() {
            Assert.AreEqual(ShipRunAbortReason.None, _session.Model.LastAbortReason, "Late: the first flight ended as a wreck.");
            Assert.AreEqual(_stationItems, LoopSmokeSpawnCounts.CountSceneObjects<ShipItem>(), "Late: the first station dropped free modules.");
            AssertModulesInstalled();
        }

        [LoopSmokeStep(LoopSmokePhase.AfterRun, 3, 10)]
        private void AssertPresetGoneInLobby() {
            Assert.AreEqual(string.Empty, SessionQueries.Resolve<IReadOnlyStagePresetSelectionModel>().SelectedPresetId, "Back in the lobby the preset is still selected.");
            Assert.IsTrue(string.IsNullOrEmpty(SessionQueries.Resolve<IReadOnlyRunStagePresetModel>().PresetId), "Back in the lobby clients still see the preset.");
            Assert.IsFalse(FindLabel().gameObject.activeInHierarchy, "Back in the lobby the preset label is still shown.");
        }

        private void AssertLabelShowsLate(string where) {
            Text label = FindLabel();
            Assert.IsTrue(label.gameObject.activeInHierarchy, $"The {where} shows no preset label.");
            Assert.AreEqual(LATE_LABEL, label.text, $"The {where} preset label.");
        }

        private static Text FindLabel() =>
            Object.FindAnyObjectByType<StagePresetLabelViewBase>().GetComponentInChildren<Text>(true);
    }
}
#endif
