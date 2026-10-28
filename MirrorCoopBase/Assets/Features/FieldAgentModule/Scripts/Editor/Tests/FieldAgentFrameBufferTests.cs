using System.Collections.Generic;
using Features.FieldAgentModule.Scripts.Agents;
using Features.FieldAgentModule.Scripts.Client;
using Features.FieldAgentModule.Scripts.Network;
using Mirror;
using NUnit.Framework;
using UnityEngine;

namespace Features.FieldAgentModule.Scripts.Editor.Tests {
    public sealed class FieldAgentFrameBufferTests {
        private const int CAPACITY = 4;
        private const float TOLERANCE = 0.001f;

        private FieldAgentFrameBuffer _buffer;
        private readonly List<FieldAgentPose> _poses = new();

        [SetUp]
        public void SetUp() =>
            _buffer = new FieldAgentFrameBuffer(CAPACITY);

        [Test]
        public void WhenRenderTimeIsBetweenTwoBatches_ThenPositionAndRotationAreInterpolated() {
            _buffer.Add(CreateFrame(1, 1d, CreateAgent(5, FieldAgentMode.Flight, Vector3.zero, 0f)));
            _buffer.Add(CreateFrame(2, 2d, CreateAgent(5, FieldAgentMode.Flight, new Vector3(10f, 0f, 0f), 90f)));

            _buffer.Sample(1.25d, _poses);

            Assert.AreEqual(1, _poses.Count);
            Assert.AreEqual(2.5f, _poses[0].Position.x, TOLERANCE);
            Assert.AreEqual(22.5f, _poses[0].Rotation.eulerAngles.y, 0.5f);
        }

        [Test]
        public void WhenTheModeChangesBetweenBatches_ThenTheNewerPoseIsShownInItsOwnSpace() {
            _buffer.Add(CreateFrame(1, 1d, CreateAgent(5, FieldAgentMode.Flight, new Vector3(500f, 0f, 0f), 0f)));
            _buffer.Add(CreateFrame(2, 2d, CreateAgent(5, FieldAgentMode.Landing, new Vector3(0f, 3f, -5f), 0f)));

            _buffer.Sample(1.5d, _poses);

            Assert.AreEqual(FieldAgentMode.Landing, _poses[0].Mode);
            Assert.AreEqual(new Vector3(0f, 3f, -5f), _poses[0].Position);
        }

        [Test]
        public void WhenAnAgentIsNewInTheLaterBatch_ThenItIsShownAtItsNewPoseAndAGoneAgentIsDropped() {
            _buffer.Add(CreateFrame(1, 1d, CreateAgent(1, FieldAgentMode.Flight, Vector3.zero, 0f), CreateAgent(2, FieldAgentMode.Flight, Vector3.one, 0f)));
            _buffer.Add(CreateFrame(2, 2d, CreateAgent(2, FieldAgentMode.Flight, new Vector3(3f, 1f, 1f), 0f), CreateAgent(3, FieldAgentMode.Flight, new Vector3(7f, 0f, 0f), 0f)));

            _buffer.Sample(1.5d, _poses);

            Assert.AreEqual(2, _poses.Count);
            Assert.AreEqual(2, _poses[0].Id);
            Assert.AreEqual(2f, _poses[0].Position.x, TOLERANCE);
            Assert.AreEqual(3, _poses[1].Id);
            Assert.AreEqual(7f, _poses[1].Position.x, TOLERANCE);
        }

        [Test]
        public void WhenRenderTimeIsPastTheNewestOrBeforeTheOldest_ThenThatBatchIsShownWithoutExtrapolation() {
            _buffer.Add(CreateFrame(1, 1d, CreateAgent(1, FieldAgentMode.Flight, Vector3.zero, 0f)));
            _buffer.Add(CreateFrame(2, 2d, CreateAgent(1, FieldAgentMode.Flight, new Vector3(10f, 0f, 0f), 0f)));

            _buffer.Sample(5d, _poses);
            Assert.AreEqual(10f, _poses[0].Position.x, TOLERANCE);

            _buffer.Sample(0.5d, _poses);
            Assert.AreEqual(0f, _poses[0].Position.x, TOLERANCE);
        }

        [Test]
        public void WhenAnOlderOrRepeatedBatchArrives_ThenItIsIgnored() {
            _buffer.Add(CreateFrame(2, 2d, CreateAgent(1, FieldAgentMode.Flight, new Vector3(10f, 0f, 0f), 0f)));
            _buffer.Add(CreateFrame(1, 1d, CreateAgent(1, FieldAgentMode.Flight, Vector3.zero, 0f)));
            _buffer.Add(CreateFrame(2, 2d, CreateAgent(1, FieldAgentMode.Flight, Vector3.zero, 0f)));

            Assert.AreEqual(1, _buffer.Count);
        }

        [Test]
        public void WhenMoreBatchesArriveThanItKeeps_ThenTheOldestGoesAndInterpolationStillWorks() {
            for (int i = 1; i <= CAPACITY + 3; i++)
                _buffer.Add(CreateFrame(i, i, CreateAgent(1, FieldAgentMode.Flight, new Vector3(i, 0f, 0f), 0f)));

            _buffer.Sample(6.5d, _poses);

            Assert.AreEqual(CAPACITY, _buffer.Count);
            Assert.AreEqual(6.5f, _poses[0].Position.x, TOLERANCE);
        }

        [Test]
        public void WhenTheWorldShifts_ThenFlyingAgentsMoveAndShipSpaceAgentsStay() {
            _buffer.Add(CreateFrame(1, 1d, CreateAgent(1, FieldAgentMode.Flight, new Vector3(100f, 0f, 0f), 0f), CreateAgent(2, FieldAgentMode.Deck, new Vector3(1f, 0.3f, 2f), 0f)));

            _buffer.Shift(new Vector3(-100f, 0f, 0f));
            _buffer.Sample(1d, _poses);

            Assert.AreEqual(0f, _poses[0].Position.x, TOLERANCE);
            Assert.AreEqual(new Vector3(1f, 0.3f, 2f), _poses[1].Position);
        }

        private static FieldAgentFrame CreateFrame(int sequence, double time, params FieldAgentSnapshot[] agents) =>
            new(sequence, time, agents);

        private static FieldAgentSnapshot CreateAgent(int id, FieldAgentMode mode, Vector3 position, float yaw) =>
            new(id, 0, (byte)mode, position, Compression.CompressQuaternion(Quaternion.Euler(0f, yaw, 0f)));
    }
}
