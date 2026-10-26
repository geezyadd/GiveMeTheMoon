#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.ShipImpactModule.Scripts;
using Features.ShipImpactModule.Scripts.Debug;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Debug;
using Features.ShipModule.Scripts.Generated;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // Rock collisions on the host in cruise, through the debug window's rock aim entry (it puts the ship before a
    // rock and turns it there; the hit itself is the real server check). A small rock: it breaks once (broken list,
    // RockDestroyed once), the synced hull drops, every peer gets a small impact. A large rock: a large impact, the
    // rock stays, the ship is pushed back out and never gets inside it, and the player still rides on the deck.
    // Then the ship goes back to where it was and faces the destination again.
    public sealed class LoopSmokeRockImpactCheck {
        private const float HIT_TIMEOUT_SECONDS = 8f;
        private const float PUSH_WINDOW_SECONDS = 2.5f;
        private const float MIN_PUSH_BACK_METERS = 0.5f;
        private const float BELOW_DECK_TOLERANCE = 0.25f;

        private readonly LoopSmokeContext _context;
        private readonly IShipRockImpactDebug _shipRockImpactDebug;
        private readonly IShipImpactEvents _shipImpactEvents;
        private readonly IFieldRockEvents _fieldRockEvents;
        private readonly IFieldRockQuery _fieldRockQuery;
        private readonly IReadOnlyLiveFieldModel _liveFieldModel;
        private readonly IReadOnlyShipStatsModel _shipStatsModel;
        private readonly List<ShipRockImpact> _impacts = new List<ShipRockImpact>();
        private readonly List<FieldRockDestroyed> _destroyed = new List<FieldRockDestroyed>();

        public LoopSmokeRockImpactCheck(LoopSmokeContext context) {
            _context = context;
            _shipRockImpactDebug = SessionQueries.Resolve<IShipRockImpactDebug>();
            _shipImpactEvents = SessionQueries.Resolve<IShipImpactEvents>();
            _fieldRockEvents = SessionQueries.Resolve<IFieldRockEvents>();
            _fieldRockQuery = SessionQueries.Resolve<IFieldRockQuery>();
            _liveFieldModel = SessionQueries.Resolve<IReadOnlyLiveFieldModel>();
            _shipStatsModel = SessionQueries.Resolve<IReadOnlyShipStatsModel>();
        }

        [LoopSmokeStep(LoopSmokePhase.InCruise, 1, 1, 40)]
        public IEnumerator HitSmallAndLargeRockInCruiseCoroutine() {
            ShipBase ship = _context.Session.Ship;
            Vector3 start = ship.transform.position;
            _shipImpactEvents.OnImpact += _impacts.Add;
            _fieldRockEvents.RockDestroyed += _destroyed.Add;
            try {
                yield return HitSmallRockCoroutine();
                yield return HitLargeRockCoroutine(ship);
            }
            finally {
                _shipImpactEvents.OnImpact -= _impacts.Add;
                _fieldRockEvents.RockDestroyed -= _destroyed.Add;
            }

            yield return _context.Flight.PutShipBackCoroutine(start);
        }

        private IEnumerator HitSmallRockCoroutine() {
            float hullBefore = _shipStatsModel.Hull;
            Assert.IsTrue(_shipRockImpactDebug.TryDebugAimAtNearestRock(ShipRockImpactKind.Small, out FieldRockDescription aimed), "Rock impact: no small rock near the ship.");

            yield return WaitForCoroutine(HasBrokenRockByCollision, "a small rock broken by the ship");
            yield return null;

            FieldRockDestroyed broken = _destroyed.First(IsCollision);
            Assert.AreEqual(1, _destroyed.Count(destroyed => destroyed.Id.Equals(broken.Id)), $"Rock impact: RockDestroyed fired more than once for {broken.Id}.");
            Assert.IsTrue(_liveFieldModel.BrokenRocks.Contains(broken.Id), $"Rock impact: rock {broken.Id} is not in the synced broken list.");
            Assert.Less(_shipStatsModel.Hull, hullBefore, "Rock impact: a small rock did not damage the synced hull.");
            Assert.IsTrue(_impacts.Any(impact => impact.Kind == ShipRockImpactKind.Small), "Rock impact: no small impact reached this peer.");
            Debug.Log($"[LoopSmoke] small rock hit: aimed {aimed.Id}, broke {broken.Id} ({broken.Diameter:0.0} m), hull {hullBefore:0.0} -> {_shipStatsModel.Hull:0.0}");
        }

        private IEnumerator HitLargeRockCoroutine(ShipBase ship) {
            Assert.IsTrue(_shipRockImpactDebug.TryDebugAimAtNearestRock(ShipRockImpactKind.Large, out FieldRockDescription rock), "Rock impact: no large rock near the ship.");

            yield return WaitForCoroutine(HasLargeImpact, "a large rock knocking the ship back");
            // The ship brakes towards the rock first (the acceleration is limited), then moves back out.
            float atHit = HorizontalDistance(ship.transform.position, rock.Center);
            float nearest = atHit;
            float backOut = atHit;
            for (float time = 0f; time < PUSH_WINDOW_SECONDS; time += Time.deltaTime) {
                yield return null;
                float distance = HorizontalDistance(ship.transform.position, rock.Center);
                if (distance < nearest) {
                    nearest = distance;
                    backOut = distance;
                }

                backOut = Mathf.Max(backOut, distance);
            }

            float contactRadius = SessionQueries.Resolve<IShipRockContactRule>().CalculateContactRadius(ShipRockImpactKind.Large, rock.Radius);
            Assert.Greater(nearest, contactRadius, $"Rock impact: the ship got inside the large rock {rock.Id} (contact radius {contactRadius:0.0} m, nearest {nearest:0.0} m).");
            Assert.Greater(backOut, nearest + MIN_PUSH_BACK_METERS, $"Rock impact: the large rock {rock.Id} did not push the ship back out (hit at {atHit:0.0} m, nearest {nearest:0.0} m, back to {backOut:0.0} m).");
            Assert.IsTrue(_fieldRockQuery.TryGetRock(rock.Id, out _), $"Rock impact: the large rock {rock.Id} is gone.");
            Assert.IsFalse(_liveFieldModel.BrokenRocks.Contains(rock.Id), $"Rock impact: the large rock {rock.Id} was broken.");
            Assert.IsTrue(SessionQueries.LocalPlayer.GetComponent<ShipRider>().IsRiding, "Rock impact: the player stopped riding on a large rock hit.");
            AssertPlayerStandsOnDeck(ship);
            Debug.Log($"[LoopSmoke] large rock hit: {rock.Id} ({rock.SourceId}, {rock.Diameter:0.0} m), distance {atHit:0.0} -> nearest {nearest:0.0} -> back to {backOut:0.0} m");
        }

        // In ship space: the world bounds of the deck collider do not cover the whole walkable deck once the ship turns.
        private static void AssertPlayerStandsOnDeck(ShipBase ship) {
            Vector3 player = SessionQueries.LocalPlayer.transform.position;
            Vector3 local = Quaternion.Inverse(ship.transform.rotation) * (player - ship.transform.position);
            Assert.IsTrue(ship.DeckGeometry.ContainsDeckWalk(local, 0f), $"Rock impact: the player at ship-local {local} is off the deck after a large rock hit.");
            Assert.IsTrue(ship.DeckGeometry.TryGetDeckSurfaceY(local, out float surfaceY), $"Rock impact: no deck under the player at ship-local {local}.");
            Assert.Greater(local.y, surfaceY - BELOW_DECK_TOLERANCE, $"Rock impact: the player at ship-local {local} is below the deck ({surfaceY:0.00}).");
        }

        private static IEnumerator WaitForCoroutine(System.Func<bool> isDone, string what) {
            for (float time = 0f; !isDone(); time += Time.deltaTime) {
                Assert.Less(time, HIT_TIMEOUT_SECONDS, $"Rock impact: timed out waiting for {what}.");
                yield return null;
            }
        }

        private bool HasBrokenRockByCollision() =>
            _destroyed.Any(IsCollision);

        private bool HasLargeImpact() =>
            _impacts.Any(impact => impact.Kind == ShipRockImpactKind.Large);

        private static bool IsCollision(FieldRockDestroyed destroyed) =>
            destroyed.Cause == FieldRockDestroyCause.ShipCollision;

        private static float HorizontalDistance(Vector3 a, Vector3 b) =>
            new Vector2(a.x - b.x, a.z - b.z).magnitude;
    }
}
#endif
