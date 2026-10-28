using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Configuration;
using Features.FieldAgentModule.Scripts.Spawn;
using Features.FlightFieldModule.Contracts;
using Features.SpaceHazeModule.Scripts.Configurations;
using NUnit.Framework;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Editor.Tests {
    public sealed class FieldAgentHiddenSpawnTests {
        private const int SEEDS = 20;

        private FieldAgentConfiguration _configuration;
        private SpaceHazeConfiguration _haze;
        private FieldRockSphereQueryFake _rocks;
        private FieldAgentHiddenSpawnService _service;
        private readonly List<Vector3> _cameras = new() { new Vector3(-4f, 2f, 0f), new Vector3(4f, 2f, 1f), new Vector3(0f, 3f, -3f) };

        [SetUp]
        public void SetUp() {
            _configuration = FieldAgentTestAssets.CreateConfiguration();
            _haze = FieldAgentTestAssets.CreateHaze();
            _rocks = new FieldRockSphereQueryFake();
            _service = new FieldAgentHiddenSpawnService(_rocks, _configuration, _haze);
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(_configuration);
            Object.DestroyImmediate(_haze);
        }

        [Test]
        public void WhenARockStandsBetweenEveryCameraAndThePoint_ThenThePointIsHiddenByTheRock() {
            _rocks.Add(new Vector3(0f, 2f, 60f), 15f);

            Assert.IsTrue(_service.IsHiddenByRock(new Vector3(0f, 2f, 100f), _cameras));
        }

        [Test]
        public void WhenOneCameraSeesPastTheRock_ThenThePointIsNotHidden() {
            _rocks.Add(new Vector3(0f, 2f, 60f), 15f);
            List<Vector3> cameras = new(_cameras) { new Vector3(60f, 2f, 0f) };

            Assert.IsFalse(_service.IsHiddenByRock(new Vector3(0f, 2f, 100f), cameras));
        }

        [Test]
        public void WhenTheRockIsAroundThePointItself_ThenItDoesNotHideIt() {
            _rocks.Add(new Vector3(0f, 2f, 100f), 15f);

            Assert.IsFalse(_service.IsHiddenByRock(new Vector3(0f, 2f, 100f), _cameras));
        }

        [Test]
        public void WhenRocksStandAround_ThenEveryFoundPointIsClearOfRocksAndHiddenFromEveryCamera() {
            AddRockRing();

            for (int seed = 1; seed <= SEEDS; seed++) {
                Assert.IsTrue(_service.TryFindHiddenPoint(Vector3.zero, _cameras, seed, out Vector3 point, out FieldAgentSpawnCover cover), $"Seed {seed}: no hidden point.");
                Assert.AreEqual(FieldAgentSpawnCover.Rock, cover, $"Seed {seed}: the rocks should hide the point.");
                Assert.IsTrue(_service.IsClearOfRocks(point), $"Seed {seed}: {point} is in a rock.");
                AssertEveryCameraLooksThroughARock(point, seed);
            }
        }

        [Test]
        public void WhenNoRockHides_ThenThePointLiesPastTheHazeForEveryCamera() {
            Assert.IsTrue(_service.TryFindHiddenPoint(Vector3.zero, _cameras, 3, out Vector3 point, out FieldAgentSpawnCover cover));

            Assert.AreEqual(FieldAgentSpawnCover.Haze, cover);
            foreach (Vector3 camera in _cameras)
                Assert.GreaterOrEqual(Vector3.Distance(camera, point), _haze.HiddenMeters, $"Camera {camera} sees {point} through the haze.");
        }

        // The ring of rocks at 100-120 m hides the shell behind it from cameras in the middle.
        private void AddRockRing() {
            for (int angle = 0; angle < 360; angle += 12) {
                Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
                _rocks.Add(direction * 110f, 14f);
            }
        }

        private void AssertEveryCameraLooksThroughARock(Vector3 point, int seed) {
            foreach (Vector3 camera in _cameras) {
                Assert.IsTrue(IsSegmentThroughAnyRock(camera, point), $"Seed {seed}: camera {camera} sees the spawn point {point}.");
            }
        }

        private bool IsSegmentThroughAnyRock(Vector3 from, Vector3 to) {
            List<FieldRockDescription> rocks = new();
            _rocks.FindInSphere((from + to) * 0.5f, Vector3.Distance(from, to) * 0.5f, rocks);
            foreach (FieldRockDescription rock in rocks) {
                Vector3 closest = from + Vector3.Project(rock.Center - from, to - from);
                if (Vector3.Distance(closest, rock.Center) < rock.Radius)
                    return true;
            }

            return false;
        }
    }
}
