#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections;
using System.Globalization;
using Features.CameraModule.Scripts;
using Features.FlightFieldModule.Contracts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Tests.Coop.Shared;
using Tests.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.Coop.Client {
    // The economy and weapons scenarios of the client bot (CoopEconomyTest, CoopWeaponsTest): it joins, boards and then
    // does what the host commands, one command after another, each through the real input path, and reports what its
    // own peer sees after each one. The host decides the order and pass or fail.
    public sealed class CoopWeaponsEconomyClientScenarios {
        private const float SETTLE_SECONDS = 1.5f;

        private readonly CoopTestClient _client;
        private readonly CoopClientCommands _commands;
        private readonly CoopClientShop _shop;
        private readonly CoopClientShipWork _shipWork;
        private readonly CoopClientWeapons _weapons;
        private readonly WaitForSeconds _settle = new WaitForSeconds(SETTLE_SECONDS);

        private CoopWeaponsEconomyClientScenarios(CoopTestClient client, CoopTestExchange exchange, string exchangeDirectory) {
            _client = client;
            _commands = new CoopClientCommands(client, exchange);
            CoopClientHands hands = new CoopClientHands(client);
            CoopClientScreenshots screenshots = new CoopClientScreenshots(exchangeDirectory);
            _shop = new CoopClientShop(client, hands, screenshots);
            _shipWork = new CoopClientShipWork(client, hands);
            _weapons = new CoopClientWeapons(client, hands, _commands, screenshots);
        }

        public static bool TryCreate(string scenarioName, CoopTestClient client, CoopTestExchange exchange, string exchangeDirectory, out IEnumerator scenario) {
            scenario = null;
            if (scenarioName != CoopWeaponsEconomySteps.SCENARIO_ECONOMY && scenarioName != CoopWeaponsEconomySteps.SCENARIO_WEAPONS)
                return false;

            scenario = new CoopWeaponsEconomyClientScenarios(client, exchange, exchangeDirectory).RunCoroutine();
            return true;
        }

        private IEnumerator RunCoroutine() {
            yield return _client.JoinLobbyCoroutine();
            yield return _client.WaitForCoroutine(SessionQueries.IsGameAtStation, "GameScene at the start station");
            yield return BoardCoroutine();
            CameraLookDriver.DebugPitchOverride = true;
            while (true) {
                yield return _commands.WaitForNextCoroutine();
                if (_commands.Name == CoopWeaponsEconomySteps.COMMAND_FINISH)
                    break;

                yield return RunCommandCoroutine(_commands.Name, _commands.Arguments);
            }

            CameraLookDriver.DebugPitchOverride = false;
            yield return FinishCoroutine();
        }

        private IEnumerator RunCommandCoroutine(string command, string[] arguments) {
            switch (command) {
                case CoopWeaponsEconomySteps.COMMAND_TRY_UNAFFORDABLE:
                    return _shop.TryUnaffordableCoroutine(arguments[0]);
                case CoopWeaponsEconomySteps.COMMAND_BUY:
                    return _shop.BuyCoroutine(arguments[0]);
                case CoopWeaponsEconomySteps.COMMAND_GRANT_MONEY:
                    return _shop.GrantMoneyCoroutine();
                case CoopWeaponsEconomySteps.COMMAND_LEAVE_SHOP:
                    return _shop.LeaveCoroutine();
                case CoopWeaponsEconomySteps.COMMAND_INSTALL:
                    return _shipWork.InstallCoroutine(arguments[0], arguments[1]);
                case CoopWeaponsEconomySteps.COMMAND_SEE_DAMAGE:
                    return _shipWork.SeeDamageCoroutine(arguments[0], arguments[1]);
                case CoopWeaponsEconomySteps.COMMAND_REPAIR_MODULE:
                    return _shipWork.RepairModuleCoroutine(arguments[0]);
                case CoopWeaponsEconomySteps.COMMAND_PATCH_BREACH:
                    return _shipWork.PatchBreachCoroutine(arguments[0]);
                case CoopWeaponsEconomySteps.COMMAND_BOARD:
                    return BoardCoroutine();
                case CoopWeaponsEconomySteps.COMMAND_RIDE:
                    return RideCoroutine();
                case CoopWeaponsEconomySteps.COMMAND_LAND:
                    return LandCoroutine();
                case CoopWeaponsEconomySteps.COMMAND_FIRE_PISTOL_AT_DUMMY:
                    return _weapons.FirePistolAtDummyCoroutine();
                case CoopWeaponsEconomySteps.COMMAND_DROP_PISTOL:
                    return _weapons.DropPistolCoroutine();
                case CoopWeaponsEconomySteps.COMMAND_PICK_UP_PISTOL:
                    return _weapons.PickUpPistolCoroutine();
                case CoopWeaponsEconomySteps.COMMAND_SIT_AT_TURRET:
                    return _weapons.SitAtTurretCoroutine(arguments[0]);
                case CoopWeaponsEconomySteps.COMMAND_FIRE_TURRET_AT_DUMMY:
                    return _weapons.FireTurretAtDummyCoroutine();
                case CoopWeaponsEconomySteps.COMMAND_STAND_UP:
                    return _weapons.StandUpCoroutine();
                case CoopWeaponsEconomySteps.COMMAND_TURRET_ROCK:
                    return _weapons.TurretShootRockCoroutine(FieldRockIdText.Parse(arguments, 0));
                case CoopWeaponsEconomySteps.COMMAND_TURRET_BALLOON:
                    return _weapons.TurretShootBalloonCoroutine(ReadBalloon(arguments));
                case CoopWeaponsEconomySteps.COMMAND_PISTOL_ROCK:
                    return _weapons.PistolShootRockCoroutine(FieldRockIdText.Parse(arguments, 0));
                case CoopWeaponsEconomySteps.COMMAND_PISTOL_BALLOON:
                    return _weapons.PistolShootBalloonCoroutine(ReadBalloon(arguments));
                case CoopWeaponsEconomySteps.COMMAND_SEE_BALLOON:
                    return _weapons.SeeBalloonPoppedCoroutine(ReadBalloon(arguments));
                default:
                    return _client.FailCoroutine($"Unknown host command '{command}'.");
            }
        }

        // At the stern: the host stands on the deck's middle, and a client dropped onto its head stands 2 m up.
        private IEnumerator BoardCoroutine() {
            ShipDeckBoarding.PutAboveStern(CoopClientHands.Body, Object.FindAnyObjectByType<ShipBase>());
            yield return _settle;
            _client.Report(CoopWeaponsEconomyObserver.Capture(CoopWeaponsEconomySteps.BOARDED));
        }

        // The flight the host launches next, reported in its cruise; the client goes on taking commands there.
        private IEnumerator RideCoroutine() {
            IReadOnlyShipRunModel run = SessionQueries.Resolve<IReadOnlyShipRunModel>();
            yield return _client.WaitForCoroutine(() => run.Phase == ShipRunPhase.Cruise, $"the cruise of loop {run.LoopIndex + 1}");
            _client.Report(CoopWeaponsEconomyObserver.Capture(CoopWeaponsEconomySteps.IN_CRUISE));
        }

        // In flight the loop index still counts the flights finished before this one.
        private IEnumerator LandCoroutine() {
            IReadOnlyShipRunModel run = SessionQueries.Resolve<IReadOnlyShipRunModel>();
            int landedLoop = run.LoopIndex + 1;
            yield return _client.WaitForCoroutine(() => SessionQueries.IsAtStationAfterLoop(landedLoop), $"Build after loop {landedLoop}");
            yield return _settle;
            _client.Report(CoopWeaponsEconomyObserver.Capture(CoopWeaponsEconomySteps.LANDED));
        }

        private IEnumerator FinishCoroutine() {
            CoopSessionActions.LeaveThroughHud();
            yield return _client.WaitForCoroutine(SessionQueries.IsBackInMenu, "the menu after the last step");
            CoopObservation observation = CoopObserver.CaptureStep(CoopTestSteps.FINISHED);
            observation.Errors = _client.CollectErrors();
            _client.Report(observation);
            Application.Quit();
        }

        private static FieldTargetId ReadBalloon(string[] arguments) =>
            new FieldTargetId(int.Parse(arguments[0], CultureInfo.InvariantCulture));
    }
}
#endif
