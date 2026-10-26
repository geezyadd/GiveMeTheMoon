#if UNITY_INCLUDE_TESTS
using System.Collections;
using Features.CharacterMovableModule.Scripts;
using Features.FloatingControllerModule;
using Features.GrabModule.Scripts;
using Features.PlayerLifeModule.Scripts;
using Features.ShipModule.Scripts;
using Features.TurretModule.Scripts;
using Mirror;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // The hull is lost in flight with the host seated at the minigun on the deck (run 2 ends in its sixth cruise): back
    // in the lobby the host is free of the ship (not riding, not seated, its body under physics again), stands on the
    // lobby's ground and walks under the move input; in the next run it walks on the deck.
    public sealed class LoopSmokeHullLossCheck {
        private const int RUN = 2;
        private const int FLIGHT = 6;
        private const float SEAT_TIMEOUT_SECONDS = 3f;
        private const float BOARD_SETTLE_SECONDS = 0.6f;
        private const float DROP_HEIGHT = 0.5f;
        private const float LOBBY_SETTLE_SECONDS = 1f;
        private const float GROUND_PROBE_UP = 0.5f;
        private const float GROUND_PROBE_DISTANCE = 4f;
        private const float MAX_STANDING_DROP = 0.25f;
        private const float WALK_SECONDS = 0.4f;
        private const float MIN_WALK_METERS = 1f;
        private const int GROUND_HIT_CAPACITY = 8;

        private readonly LoopSmokeSession _session;
        private readonly RaycastHit[] _groundHits = new RaycastHit[GROUND_HIT_CAPACITY];

        private static NetworkIdentity Player => SessionQueries.LocalPlayer;

        public LoopSmokeHullLossCheck(LoopSmokeContext context) =>
            _session = context.Session;

        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, RUN, FLIGHT, 10)]
        public IEnumerator StandByMinigunCoroutine() {
            ShipDeckBoarding.PutAbove(Player.GetComponent<CharacterMovableBase>(), _session.Ship, LoopSmokeTurretCheck.GunnerSpot, DROP_HEIGHT);
            yield return new WaitForSeconds(BOARD_SETTLE_SECONDS);
        }

        // The run ends right after this cruise slot: the hull is destroyed with the host still seated.
        [LoopSmokeStep(LoopSmokePhase.InCruise, RUN, FLIGHT, 10)]
        public IEnumerator SitAtMinigunCoroutine() {
            ShipSocket socket = LoopSmokeTurretCheck.FindSocket(_session.Ship);
            ShipTurretStation station = socket.GetComponentInChildren<ShipTurretStation>();
            Player.GetComponent<UseController>().RequestUse(socket.GetComponentInChildren<ShipSitInteractable>());
            yield return _session.WaitForCoroutine(() => station.IsOccupiedByLocalPlayer, SEAT_TIMEOUT_SECONDS, "Hull loss: the host seated at the minigun");

            ShipRider rider = Player.GetComponent<ShipRider>();
            Assert.IsTrue(rider.IsRiding && rider.IsSeated, "Hull loss: the host does not ride the ship seated before the hull is destroyed.");
        }

        [LoopSmokeStep(LoopSmokePhase.AfterRun, RUN, 10)]
        public IEnumerator AssertFreeAndWalkingInLobbyCoroutine() {
            AssertReleasedFromShip("in the lobby");
            yield return new WaitForSeconds(LOBBY_SETTLE_SECONDS);
            float settledY = Player.transform.position.y;
            yield return new WaitForSeconds(LOBBY_SETTLE_SECONDS);
            Vector3 position = Player.transform.position;
            Assert.Less(settledY - position.y, MAX_STANDING_DROP, $"Hull loss: the host is falling in the lobby: {settledY} -> {position.y}.");
            AssertStandsOnLobbyGround(position);

            float walked = 0f;
            yield return WalkCoroutine(Vector2.up, distance => walked = distance);
            Assert.GreaterOrEqual(walked, MIN_WALK_METERS, "Hull loss: the host does not walk in the lobby under the move input.");
        }

        [LoopSmokeStep(LoopSmokePhase.BeforeTakeoff, RUN + 1, 1, 20)]
        public IEnumerator AssertWalkingOnDeckInNextRunCoroutine() {
            AssertReleasedFromShip("at the next run's first station");
            float walked = 0f;
            yield return WalkCoroutine(Vector2.up, distance => walked = distance);
            Assert.GreaterOrEqual(walked, MIN_WALK_METERS, "Hull loss: the host does not walk on the deck in the next run.");
            yield return WalkCoroutine(Vector2.down, _ => { });
            yield return new WaitForSeconds(BOARD_SETTLE_SECONDS);
            Assert.IsTrue(SessionQueries.IsOnDeck(_session.Ship, Player.transform.position), "Hull loss: the host did not stay on the deck after walking in the next run.");
        }

        private static void AssertReleasedFromShip(string where) {
            ShipRider rider = Player.GetComponent<ShipRider>();
            Rigidbody body = Player.GetComponent<Rigidbody>();
            Assert.IsFalse(rider.IsRiding, $"Hull loss: the host still rides a ship {where}.");
            Assert.IsFalse(rider.IsSeated, $"Hull loss: the host is still seated {where}.");
            Assert.IsFalse(body.isKinematic, $"Hull loss: the host's body is kinematic {where}.");
            Assert.IsTrue(body.detectCollisions, $"Hull loss: the host's body has its collisions off {where}.");
            Assert.IsTrue(Player.GetComponent<FloatingController>().HoverEnabled, $"Hull loss: the host's hover is off {where}.");
            Assert.IsTrue(Player.GetComponent<NetworkRigidbodyUnreliable>().enabled, $"Hull loss: the host's network body is silenced {where}.");
            Assert.IsFalse(Player.GetComponent<PlayerLifeBody>().Damageable.IsDead, $"Hull loss: the host is dead {where}.");
            Assert.IsFalse(SessionQueries.Resolve<CharacterInputBuffer>().IsBlocked, $"Hull loss: the host's controls are blocked {where}.");
        }

        private void AssertStandsOnLobbyGround(Vector3 position) {
            string lobby = SessionQueries.NetworkManager.LobbySceneName;
            int count = Physics.RaycastNonAlloc(position + Vector3.up * GROUND_PROBE_UP, Vector3.down, _groundHits, GROUND_PROBE_DISTANCE, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++) {
                Collider ground = _groundHits[i].collider;
                if (ground.transform.IsChildOf(Player.transform) == false && ground.gameObject.scene.name == lobby)
                    return;
            }

            Assert.Fail($"Hull loss: the host at {position} does not stand on the lobby's ground (in the void).");
        }

        // Through the same input buffer the keyboard fills; the distance walked on the ground plane.
        private static IEnumerator WalkCoroutine(Vector2 move, System.Action<float> walked) {
            CharacterInputBuffer input = SessionQueries.Resolve<CharacterInputBuffer>();
            Vector3 start = Player.transform.position;
            input.BeginScripted();
            input.SetScripted(move, false, false);
            yield return new WaitForSeconds(WALK_SECONDS);
            input.EndScripted();
            Vector3 delta = Player.transform.position - start;
            delta.y = 0f;
            walked(delta.magnitude);
        }
    }
}
#endif
