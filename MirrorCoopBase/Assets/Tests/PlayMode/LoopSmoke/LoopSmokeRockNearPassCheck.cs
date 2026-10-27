#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Features.DebugModule.Contracts;
using Features.DebugModule.Scripts.Models;
using Features.DebugModule.Scripts.UI;
using Features.FlightFieldModule.Contracts;
using Features.FlightFieldModule.Scripts.Network.Generated;
using Features.ShipImpactModule.Scripts;
using Features.ShipModule.Scripts;
using Features.ShipModule.Scripts.Contracts;
using Features.ShipModule.Scripts.Hull;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;

namespace Tests.PlayMode.LoopSmoke {
    // A near pass by a small rock, the real server check: the ship is put to fly by with its side inside the rock's
    // data sphere but outside the contact sphere (between the two, a clear gap to the visible rock). The rock may break
    // (and damage the hull) only when the hull really came within the contact sphere. The debug window's "Show impact
    // volume" toggle is clicked on for the pass as a player does and off again after it.
    public sealed class LoopSmokeRockNearPassCheck {
        private const string HIDDEN_LABEL = "Show impact volume: off";
        private const string SHOWN_LABEL = "Show impact volume: on";
        private const float CANDIDATE_SEARCH_RADIUS = 500f;
        // Farther than the longest swept move, so the debug move to the rock is not swept through other rocks (m).
        private const float MIN_CANDIDATE_DISTANCE = 90f;
        // No other rock's data sphere comes this close to the passed rock's (m).
        private const float CLEAR_AROUND_METERS = 40f;
        private const float PASS_SECONDS = 1.5f;
        // The hull's distance is read once a frame, the server checks every physics step in between (m).
        private const float STEP_TOLERANCE_METERS = 0.5f;

        private readonly LoopSmokeContext _context;
        private readonly List<FieldRockDescription> _rocks = new List<FieldRockDescription>();
        private readonly List<FieldRockDescription> _neighbours = new List<FieldRockDescription>();
        private readonly List<FieldRockDestroyed> _destroyed = new List<FieldRockDestroyed>();

        private float _nearestHullMeters = float.MaxValue;

        public LoopSmokeRockNearPassCheck(LoopSmokeContext context) =>
            _context = context;

        [LoopSmokeStep(LoopSmokePhase.InCruise, 1, 1, 45)]
        public IEnumerator PassBySmallRockWithAGapCoroutine() {
            ShipBase ship = _context.Session.Ship;
            Vector3 start = ship.transform.position;
            IFieldRockEvents rockEvents = SessionQueries.Resolve<IFieldRockEvents>();
            Assert.IsTrue(TryFindSmallRock(start, out FieldRockDescription rock), "Rock near pass: no lone small rock far enough from the ship.");
            float contactRadius = SessionQueries.Resolve<IShipRockContactRule>().CalculateContactRadius(ShipRockImpactKind.Small, rock.Radius);
            // The hull's side halfway between the contact sphere and the data sphere.
            float sideGap = (contactRadius + rock.Radius) * 0.5f;

            yield return ClickThroughDebugWindowCoroutine(HIDDEN_LABEL);
            rockEvents.RockDestroyed += _destroyed.Add;
            try {
                _context.Flight.PutShipBeside(rock.Center, ReadHullHalfWidth(ship) + sideGap, 0f);
                yield return null;
                // Up to the break, if any: after it the small push may move the hull anywhere.
                for (float time = 0f; time < PASS_SECONDS && IsDestroyed(rock.Id) == false; time += Time.deltaTime) {
                    _nearestHullMeters = Mathf.Min(_nearestHullMeters, ReadHullDistance(ship, rock.Center));
                    yield return null;
                }
            }
            finally {
                rockEvents.RockDestroyed -= _destroyed.Add;
            }

            bool isBroken = SessionQueries.Resolve<IReadOnlyLiveFieldModel>().BrokenRocks.Contains(rock.Id) || IsDestroyed(rock.Id);
            Assert.Less(_nearestHullMeters, rock.Radius, $"Rock near pass: the hull never came inside the data sphere of {rock.Id} (nearest {_nearestHullMeters:0.0} m, data radius {rock.Radius:0.0} m).");
            if (isBroken)
                Assert.LessOrEqual(_nearestHullMeters, contactRadius + STEP_TOLERANCE_METERS, $"Rock near pass: the rock {rock.Id} ({rock.Diameter:0.0} m) broke, but the hull came only {_nearestHullMeters:0.0} m near its middle (contact radius {contactRadius:0.0} m).");

            Debug.Log($"[LoopSmoke] small rock near pass: {rock.Id} ({rock.Diameter:0.0} m), hull {_nearestHullMeters:0.0} m from its middle at the nearest, contact radius {contactRadius:0.0} m, data radius {rock.Radius:0.0} m, broken {isBroken}");
            yield return ClickThroughDebugWindowCoroutine(SHOWN_LABEL);
            yield return _context.Flight.PutShipBackCoroutine(start);
        }

        // The biggest small rock in reach with no other rock near it: the gap between its contact and data spheres is
        // the widest, and no neighbour can knock the ship into it on the way.
        public bool TryFindSmallRock(Vector3 from, out FieldRockDescription found) {
            found = default;
            float biggest = 0f;
            IFieldRockQuery query = SessionQueries.Resolve<IFieldRockQuery>();
            query.FindInSphere(from, CANDIDATE_SEARCH_RADIUS, _rocks);
            foreach (FieldRockDescription rock in _rocks) {
                bool isSmall = rock.Size == FieldRockSize.Small && rock.IsBreakable;
                if (isSmall == false || Vector3.Distance(from, rock.Center) < MIN_CANDIDATE_DISTANCE || rock.Diameter <= biggest)
                    continue;

                if (IsAlone(query, rock) == false)
                    continue;

                biggest = rock.Diameter;
                found = rock;
            }

            return biggest > 0f;
        }

        private bool IsAlone(IFieldRockQuery query, FieldRockDescription rock) {
            query.FindInSphere(rock.Center, rock.Radius + CLEAR_AROUND_METERS, _neighbours);
            return _neighbours.Count == 1;
        }

        private bool IsDestroyed(FieldRockId id) =>
            _destroyed.Exists(destroyed => destroyed.Id.Equals(id));

        // From the point to the nearest hull box, in ship space (m).
        public static float ReadHullDistance(ShipBase ship, Vector3 point) {
            Vector3 local = ship.transform.InverseTransformPoint(point);
            float nearest = float.MaxValue;
            foreach (ShipHullBox box in ship.HullVolume.Boxes) {
                Vector3 offset = local - box.Center;
                Vector3 outside = new Vector3(
                    Mathf.Max(0f, Mathf.Abs(offset.x) - box.HalfExtents.x),
                    Mathf.Max(0f, Mathf.Abs(offset.y) - box.HalfExtents.y),
                    Mathf.Max(0f, Mathf.Abs(offset.z) - box.HalfExtents.z));
                nearest = Mathf.Min(nearest, outside.magnitude);
            }

            return nearest;
        }

        // From the ship's middle line to the farthest side of its hull boxes (m).
        public static float ReadHullHalfWidth(ShipBase ship) {
            float halfWidth = 0f;
            foreach (ShipHullBox box in ship.HullVolume.Boxes)
                halfWidth = Mathf.Max(halfWidth, Mathf.Abs(box.Center.x) + box.HalfExtents.x);

            return halfWidth;
        }

        private static IEnumerator ClickThroughDebugWindowCoroutine(string label) {
            DebugPanelModel panel = SessionQueries.Resolve<DebugPanelModel>();
            DebugToggleView toggle = Object.FindAnyObjectByType<DebugToggleView>();
            Assert.IsNotNull(toggle, "Rock near pass: no debug corner button in the session.");
            toggle.DebugButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(panel.IsOpen, "Rock near pass: the corner button did not open the debug panel.");
            Assert.IsTrue(SessionQueries.Resolve<DebugActionsModel>().TryFind(DebugSections.ROCKS, label, out DebugActionDefinition action), $"Rock near pass: no \"{label}\" debug action registered.");
            Assert.IsTrue(Object.FindAnyObjectByType<DebugPanelView>().TryGetDebugRow(action, out DebugActionRowItem row), $"Rock near pass: the debug panel shows no \"{label}\" button.");
            Assert.IsTrue(row.DebugButton.interactable, $"Rock near pass: \"{label}\" is disabled in cruise.");
            row.DebugButton.onClick.Invoke();
            yield return null;
            toggle.DebugButton.onClick.Invoke();
            yield return null;
            Assert.IsFalse(panel.IsOpen, "Rock near pass: the corner button did not close the debug panel.");
        }
    }
}
#endif
