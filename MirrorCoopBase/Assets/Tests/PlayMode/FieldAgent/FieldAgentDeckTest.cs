#if UNITY_INCLUDE_TESTS
using System.Collections;
using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Client;
using Features.FieldAgentModule.Scripts.Debug;
using NUnit.Framework;
using Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode.FieldAgent {
    // In a cruise with the ship turning the whole time, the host clicks "Land an agent on the deck": one agent flies
    // in, lands on the deck NavMesh, walks to the helm and back and takes off again. On the deck it never leaves the
    // deck surface, as the server keeps it and as the host shows it, while the ship flies and turns under it.
    public sealed class FieldAgentDeckTest {
        private const int TEST_TIMEOUT_MS = 240000;
        private const string TRIP_LABEL = "Land an agent on the deck";
        private const float TRIP_TIMEOUT_SECONDS = 120f;
        private const float TURNING_STEER = 0.5f;
        // The baked deck NavMesh, ship-local: its surface height and its walkable rectangle.
        private const float DECK_SURFACE_Y = 0.37f;
        private const float SURFACE_TOLERANCE = 0.06f;
        private const float DECK_HALF_WIDTH = 4.2f;
        private const float DECK_STERN_Z = -10.05f;
        private const float DECK_NOSE_Z = 7.4f;
        // The host shows the agent from batches a quarter second old, in ship space, so its view stays on the deck too.
        private const float SHOWN_TOLERANCE = 0.25f;
        private const float MIN_TURN_ON_DECK_DEGREES = 30f;
        // The host shows batches this much behind the server (the interpolation delay plus one batch): until then it
        // still shows the landing.
        private const float SHOWN_DELAY_SECONDS = 0.4f;

        private FieldAgentTestFlight _flight;
        private IFieldAgentDeckTripDebug _trip;
        private readonly HashSet<FieldAgentDeckTripStage> _stages = new();
        private float _turnOnDeck;
        private float _lastYaw;
        private bool _isScreenshotTaken;
        private float _onDeckSince;

        [SetUp]
        public void SetUp() =>
            _flight = new FieldAgentTestFlight();

        [TearDown]
        public void TearDown() =>
            _flight.Dispose();

        [UnityTest, Timeout(TEST_TIMEOUT_MS)]
        public IEnumerator WhenAnAgentLandsOnTheTurningShip_AndWalksToTheHelmAndBack_ThenItStaysOnTheDeckAndTakesOff() {
            LogAssert.ignoreFailingMessages = true;
            yield return _flight.FlyIntoEndlessCruiseCoroutine();
            _flight.Ship.FlightControlDebug.SetDebugSteer(true, TURNING_STEER);
            _trip = SessionQueries.Resolve<IFieldAgentDeckTripDebug>();

            yield return _flight.ClickDebugActionCoroutine(TRIP_LABEL);
            Assert.AreEqual(FieldAgentDeckTripStage.FlyingIn, _trip.Stage, "The debug button did not start the deck trip.");
            yield return FollowTripCoroutine();

            Assert.IsTrue(_stages.IsSupersetOf(new[] { FieldAgentDeckTripStage.WalkingToModule, FieldAgentDeckTripStage.AtModule, FieldAgentDeckTripStage.WalkingBack, FieldAgentDeckTripStage.TakingOff }), $"The trip skipped a stage: {string.Join(", ", _stages)}.");
            Assert.GreaterOrEqual(_turnOnDeck, MIN_TURN_ON_DECK_DEGREES, "The ship did not turn while the agent was on the deck.");
            Assert.IsTrue(_flight.Control.TryGetAgent(_trip.AgentId, out FieldAgentState state));
            Assert.AreEqual(FieldAgentMode.Flight, state.Mode, "The agent did not take off.");
            Debug.Log($"[FieldAgents] deck trip done, the ship turned {_turnOnDeck:0} degrees under the walking agent");
            yield return _flight.LandAndLeaveCoroutine();
        }

        private IEnumerator FollowTripCoroutine() {
            IFieldAgentShownAgents shown = SessionQueries.Resolve<IFieldAgentShownAgents>();
            List<int> tripAgent = new() { _trip.AgentId };
            float deadline = Time.time + TRIP_TIMEOUT_SECONDS;
            _lastYaw = _flight.Ship.transform.eulerAngles.y;
            while (_trip.Stage != FieldAgentDeckTripStage.Orbiting) {
                Assert.Less(Time.time, deadline, $"The deck trip got stuck at {_trip.Stage}.");
                Assert.AreNotEqual(FieldAgentDeckTripStage.Lost, _trip.Stage, "The deck trip lost its agent.");
                _stages.Add(_trip.Stage);
                _flight.CheckRocks(tripAgent);
                Assert.AreEqual(0, _flight.RockHits, $"The agent flew into a rock: {_flight.FirstRockHit}.");
                CheckOnDeck(shown);
                if (_isScreenshotTaken == false && _trip.Stage == FieldAgentDeckTripStage.WalkingToModule) {
                    _isScreenshotTaken = true;
                    yield return FieldAgentTestFlight.CaptureCoroutine("agent-walking-on-deck.png");
                }

                yield return null;
            }
        }

        private void CheckOnDeck(IFieldAgentShownAgents shown) {
            float yaw = _flight.Ship.transform.eulerAngles.y;
            Assert.IsTrue(_flight.Control.TryGetAgent(_trip.AgentId, out FieldAgentState state));
            if (state.Mode != FieldAgentMode.Deck) {
                _lastYaw = yaw;
                _onDeckSince = Time.time;
                return;
            }

            _turnOnDeck += Mathf.Abs(Mathf.DeltaAngle(_lastYaw, yaw));
            _lastYaw = yaw;
            AssertOnDeckSurface(state.Position, 0f, "on the server");
            bool isShownOnDeck = Time.time - _onDeckSince > SHOWN_DELAY_SECONDS;
            if (isShownOnDeck && shown.TryGetShownWorldPosition(_trip.AgentId, out Vector3 shownWorld))
                AssertOnDeckSurface(_flight.Ship.transform.InverseTransformPoint(shownWorld), SHOWN_TOLERANCE, "as the host shows it");
        }

        private static void AssertOnDeckSurface(Vector3 local, float extra, string where) {
            Assert.AreEqual(DECK_SURFACE_Y, local.y, SURFACE_TOLERANCE + extra, $"The agent is off the deck surface {where}: ship-local {local}.");
            Assert.LessOrEqual(Mathf.Abs(local.x), DECK_HALF_WIDTH + extra, $"The agent is past the deck side {where}: ship-local {local}.");
            Assert.That(local.z, Is.InRange(DECK_STERN_Z - extra, DECK_NOSE_Z + extra), $"The agent is past the deck's end {where}: ship-local {local}.");
        }
    }
}
#endif
