#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Linq;
using Features.PlayerLifeModule.Scripts;
using Features.PlayerProfileModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Generated;
using Features.ShopModule.Scripts.Data;
using Game.Connection;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests.PlayMode.LoopSmoke {
    // Joining a running session with a bot that goes through the server's real join path (authenticator, join rule,
    // the player made once its map is loaded): a new player comes in at a station and stands on the deck; the same
    // player, dead, leaves in flight, is turned away in flight with the in-flight reason, and back at a station comes in
    // dead, buyable in the Crew list, at a deck spawn point and under the new name it brought.
    public sealed class LoopSmokeRejoinCheck {
        private const int JOIN_CONNECTION_ID = 90002;
        private const int KNOCK_CONNECTION_ID = 90003;
        private const int REJOIN_CONNECTION_ID = 90004;
        private const string CLIENT_ID = "loop-smoke-rejoin-bot";
        private const string FIRST_NAME = "RejoinBot";
        private const string RETURN_NAME = "RejoinBotBack";
        private const float SPAWN_TOLERANCE = 0.1f;

        private readonly IReadOnlyShipRunModel _shipRunModel;
        private readonly IConnectionJoinPolicy _joinPolicy = SessionQueries.Resolve<IConnectionJoinPolicy>();
        private LoopSmokeBot _bot;

        public LoopSmokeRejoinCheck(LoopSmokeContext context) =>
            _shipRunModel = context.Session.Model;

        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, 1, 2, 10)]
        public IEnumerator JoinAtStationAndDieCoroutine() {
            Assert.AreEqual(ShipRunPhase.Build, _shipRunModel.Phase, "The join check must start at a station.");
            Assert.IsTrue(_joinPolicy.IsOpenForNewPlayers, "The session is closed to new players at a station.");
            _bot = JoinAccepted(JOIN_CONNECTION_ID, FIRST_NAME);
            yield return LoopSmokeShopCheck.WaitForCoroutine(IsBotSpawned, "the new player's body after its map loaded");
            AssertAtDeckSpawnPoint("A new player");
            Assert.AreEqual(PlayerLifeState.Alive, _bot.Life.LifeState, "A new player joined dead.");

            _bot.Identity.GetComponent<IDamageable>().ServerKill(DamageType.Generic);
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => _bot.Life.LifeState == PlayerLifeState.Dead, "the joined player dead");
        }

        [LoopSmokeStep(LoopSmokePhase.InCruise, 1, 2, 10)]
        public IEnumerator LeaveAndKnockInFlightCoroutine() {
            Assert.AreEqual(ShipRunPhase.Cruise, _shipRunModel.Phase, "The in-flight check did not run in flight.");
            _bot.Leave();
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => _bot.Identity == null, "the player who left in flight removed");
            Assert.IsFalse(_joinPolicy.IsOpenForNewPlayers, "The session is open to new players in flight.");

            LoopSmokeBot knock = LoopSmokeBot.Connect(KNOCK_CONNECTION_ID, CLIENT_ID, FIRST_NAME);
            try {
                knock.Authenticate();
                Assert.IsFalse(knock.IsAuthenticated, "The server accepted a player in flight.");
                yield return LoopSmokeShopCheck.WaitForCoroutine(() => knock.AuthResponse.HasValue, "the server's answer to a join in flight");
                Assert.AreEqual(ConnectionRejectReason.InFlight, knock.AuthResponse.Value, "A join in flight was turned away for another reason.");
            } finally {
                knock.Leave();
            }
        }

        [LoopSmokeStep(LoopSmokePhase.AfterLanding, 1, 2, 10)]
        public IEnumerator ComeBackAtStationCoroutine() {
            Assert.AreEqual(ShipRunPhase.Build, _shipRunModel.Phase, "The return check must run at a station.");
            Assert.IsTrue(_joinPolicy.IsOpenForNewPlayers, "The session stays closed after landing.");
            _bot = JoinAccepted(REJOIN_CONNECTION_ID, RETURN_NAME);
            yield return LoopSmokeShopCheck.WaitForCoroutine(IsBotSpawned, "the returning player's body");
            AssertAtDeckSpawnPoint("A returning player");
            Assert.AreEqual(PlayerLifeState.Dead, _bot.Life.LifeState, "A player who left dead came back alive.");

            DeadCrewModel deadCrew = SessionQueries.Resolve<DeadCrewModel>();
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => deadCrew.Entries.Contains(_bot.Key), "the returning dead player in the Crew list");
            Assert.IsTrue(SessionQueries.Resolve<IPlayerLifeReviver>().CanServerRevive(_bot.Key), "The returning dead player cannot be bought back.");
            PlayerProfile profile = _bot.Identity.GetComponent<PlayerProfile>();
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => profile.Model.DisplayName == RETURN_NAME, $"the returning player's new name {RETURN_NAME}");

            _bot.Leave();
            yield return LoopSmokeShopCheck.WaitForCoroutine(() => _bot.Identity == null, "the returning player removed");
        }

        private static LoopSmokeBot JoinAccepted(int connectionId, string playerName) {
            LoopSmokeBot bot = LoopSmokeBot.Connect(connectionId, CLIENT_ID, playerName);
            bot.Authenticate();
            Assert.IsTrue(bot.IsAuthenticated, $"The server turned {playerName} away at a station.");
            bot.EnterMap();
            return bot;
        }

        private bool IsBotSpawned() =>
            _bot.Identity != null && _bot.Identity.isClient;

        private void AssertAtDeckSpawnPoint(string who) {
            Vector3 position = _bot.Identity.transform.position;
            float nearest = float.MaxValue;
            foreach (ShipDeckSpawnPoint point in Object.FindObjectsByType<ShipDeckSpawnPoint>(FindObjectsSortMode.None))
                nearest = Mathf.Min(nearest, Vector3.Distance(point.transform.position, position));

            Assert.Less(nearest, SPAWN_TOLERANCE, $"{who} at {position} is not at a deck spawn point.");
        }
    }
}
#endif
