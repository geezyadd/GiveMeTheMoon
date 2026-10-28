using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Steering;
using Features.FlightFieldModule.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Editor.Tests {
    public sealed class FieldAgentRockAvoidanceTests {
        private const float STEP_SECONDS = 0.02f;
        private const float FLIGHT_SECONDS = 12f;
        private const float ARRIVED_METERS = 3f;
        private const float AVOIDANCE_WEIGHT = 1.5f;
        private static readonly Vector3 _target = new(0f, 0f, 80f);

        private FieldAgentProfile _profile;
        private FieldAgentRockAvoidance _avoidance;
        private FieldAgentSteering _steering;
        private FieldRockSphereQueryFake _rocks;

        [SetUp]
        public void SetUp() {
            _profile = FieldAgentTestAssets.CreateProfile();
            _avoidance = new FieldAgentRockAvoidance();
            _steering = new FieldAgentSteering();
            _rocks = new FieldRockSphereQueryFake();
        }

        [TearDown]
        public void TearDown() =>
            Object.DestroyImmediate(_profile);

        [Test]
        public void WhenRockSphereLiesOnTheCourse_ThenAvoidanceTurnsAwayFromIt() {
            List<FieldRockDescription> rocks = new() { CreateRock(new Vector3(0.5f, 0f, 12f), 3f) };

            Vector3 push = _avoidance.CalculateAvoidance(Vector3.zero, new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 10f), rocks, _profile, 1f);

            Assert.Less(push.x, 0f, $"The rock lies right of the course, the push must go left: {push}.");
            Assert.Less(push.z, 0f, $"A head-on rock must also brake the agent: {push}.");
        }

        [Test]
        public void WhenRockSphereIsDeadAhead_ThenAvoidanceTurnsToTheAgentsSide() {
            List<FieldRockDescription> rocks = new() { CreateRock(new Vector3(0f, 0f, 12f), 3f) };

            Vector3 right = _avoidance.CalculateAvoidance(Vector3.zero, new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 10f), rocks, _profile, 1f);
            Vector3 left = _avoidance.CalculateAvoidance(Vector3.zero, new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 10f), rocks, _profile, -1f);

            Assert.Greater(right.x, 0f, $"Side +1 turns right: {right}.");
            Assert.Less(left.x, 0f, $"Side -1 turns left: {left}.");
        }

        [Test]
        public void WhenRockSphereIsBesideOrBehindTheCourse_ThenNoAvoidance() {
            List<FieldRockDescription> rocks = new() {
                CreateRock(new Vector3(20f, 0f, 10f), 3f),
                CreateRock(new Vector3(0f, 0f, -12f), 3f),
                CreateRock(new Vector3(0f, 0f, 200f), 3f),
            };

            Vector3 push = _avoidance.CalculateAvoidance(Vector3.zero, new Vector3(0f, 0f, 10f), new Vector3(0f, 0f, 10f), rocks, _profile, 1f);

            Assert.AreEqual(Vector3.zero, push);
        }

        [Test]
        public void WhenSeekingThroughARockSphere_ThenSteeringFliesRoundItWithoutEnteringAndArrives() {
            _rocks.Add(new Vector3(0f, 0f, 35f), 8f);
            FieldRockDescription rock = CreateRock(new Vector3(0f, 0f, 35f), 8f);
            float minDistance = float.MaxValue;
            Vector3 position = Vector3.zero;
            Vector3 velocity = new(0f, 0f, _profile.MaxSpeed);
            List<FieldRockDescription> found = new();

            for (float time = 0f; time < FLIGHT_SECONDS; time += STEP_SECONDS) {
                _rocks.FindInSphere(position, 40f, found);
                Vector3 seek = _steering.CalculateSeek(position, _target, 1f, _profile);
                Vector3 desired = seek + _avoidance.CalculateAvoidance(position, velocity, seek, found, _profile, 1f) * AVOIDANCE_WEIGHT;
                velocity = _steering.LimitVelocity(velocity, Vector3.ClampMagnitude(desired, _profile.MaxSpeed), _profile, STEP_SECONDS);
                position += velocity * STEP_SECONDS;
                minDistance = Mathf.Min(minDistance, Vector3.Distance(position, rock.Center));
            }

            Assert.GreaterOrEqual(minDistance, rock.Radius + _profile.Radius, $"The agent came into the rock sphere: closest {minDistance:0.00} m from its middle.");
            Assert.Less(Vector3.Distance(position, _target), ARRIVED_METERS, $"The agent did not reach the target behind the rock: at {position}.");
        }

        [Test]
        public void WhenAgentIsInsideARockSphere_ThenPushOutPutsItOnTheSurfaceAndStopsTheInwardSpeed() {
            List<FieldRockDescription> rocks = new() { CreateRock(Vector3.zero, 5f) };
            Vector3 position = new(0f, 0f, 2f);
            Vector3 velocity = new(0f, 0f, -10f);

            bool isPushed = _avoidance.PushOutOfRocks(ref position, ref velocity, rocks, _profile);

            Assert.IsTrue(isPushed);
            Assert.AreEqual(5f + _profile.Radius, position.magnitude, 0.001f);
            Assert.GreaterOrEqual(velocity.z, 0f);
        }

        [Test]
        public void WhenTurning_ThenVelocityTurnsNoFasterThanTheTurnRate() {
            Vector3 current = new(0f, 0f, 10f);

            Vector3 limited = _steering.LimitVelocity(current, new Vector3(10f, 0f, 0f), _profile, 0.1f);

            Assert.LessOrEqual(Vector3.Angle(current, limited), _profile.TurnRateDegrees * 0.1f + 0.01f);
        }

        private static FieldRockDescription CreateRock(Vector3 center, float radius) =>
            new(FieldRockId.CreateCluster(0, 0), "test_rock", center, radius * 2f, FieldRockSize.Large, false);
    }
}
