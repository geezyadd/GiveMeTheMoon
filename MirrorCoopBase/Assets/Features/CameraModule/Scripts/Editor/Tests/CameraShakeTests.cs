using System.Collections.Generic;
using Features.CameraModule.Contracts;
using Features.CameraModule.Scripts.Shake;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.CameraModule.Editor.Tests {
    public sealed class CameraShakeTests {
        private const float FRAME_SECONDS = 1f / 60f;
        private const float TOLERANCE = 0.0001f;

        private readonly List<CameraShakeProfile> _profiles = new();
        private CameraShakeModel _cameraShakeModel;
        private CameraShakeService _cameraShakeService;
        private CameraShakeSampler _cameraShakeSampler;

        [SetUp]
        public void SetUp() {
            _cameraShakeModel = new CameraShakeModel();
            _cameraShakeService = new CameraShakeService(_cameraShakeModel);
            _cameraShakeSampler = new CameraShakeSampler();
        }

        [TearDown]
        public void TearDown() {
            foreach (CameraShakeProfile profile in _profiles)
                Object.DestroyImmediate(profile);

            _profiles.Clear();
        }

        [Test]
        public void WhenWeakerShakeOfSameProfileArrives_ThenStrongerShakeKeepsGoing() {
            CameraShakeProfile profile = CreateProfile(seconds: 1f);
            _cameraShakeService.AddShake(profile, 1f);
            AdvanceFrames(6);

            _cameraShakeService.AddShake(profile, 0.2f);

            Assert.AreEqual(1, _cameraShakeModel.OneShots.Count);
            Assert.AreEqual(1f, _cameraShakeModel.OneShots[0].Strength, TOLERANCE);
            Assert.Greater(_cameraShakeModel.OneShots[0].CurrentStrength, 0.2f);
        }

        [Test]
        public void WhenStrongerShakeOfSameProfileArrives_ThenItRestartsTheShake() {
            CameraShakeProfile profile = CreateProfile(seconds: 1f);
            _cameraShakeService.AddShake(profile, 0.5f);
            AdvanceFrames(6);

            _cameraShakeService.AddShake(profile, 2f);

            Assert.AreEqual(1, _cameraShakeModel.OneShots.Count);
            Assert.AreEqual(2f, _cameraShakeModel.OneShots[0].CurrentStrength, TOLERANCE);
        }

        [Test]
        public void WhenShakesOfTwoProfilesArrive_ThenBothAreKept() {
            _cameraShakeService.AddShake(CreateProfile(seconds: 1f), 1f);
            _cameraShakeService.AddShake(CreateProfile(seconds: 1f), 0.1f);

            Assert.AreEqual(2, _cameraShakeModel.OneShots.Count);
        }

        [Test]
        public void WhenContinuousAndOneShotRunTogether_ThenPoseIsTheirSum() {
            CameraShakeProfile continuousProfile = CreateProfile(seconds: 0.2f, frequency: 16f, meters: 0.05f);
            CameraShakeProfile oneShotProfile = CreateProfile(seconds: 1f, frequency: 18f, meters: 0.1f);
            CameraShakeModel continuousOnly = new();
            CameraShakeModel oneShotOnly = new();
            StartContinuous(new CameraShakeService(continuousOnly), continuousProfile);
            StartContinuous(_cameraShakeService, continuousProfile);
            new CameraShakeService(oneShotOnly).AddShake(oneShotProfile, 1f);
            _cameraShakeService.AddShake(oneShotProfile, 1f);

            for (int i = 0; i < 9; i++) {
                _cameraShakeSampler.Advance(continuousOnly, FRAME_SECONDS);
                _cameraShakeSampler.Advance(oneShotOnly, FRAME_SECONDS);
                _cameraShakeSampler.Advance(_cameraShakeModel, FRAME_SECONDS);
            }

            CameraShakePose expected = _cameraShakeSampler.Sample(continuousOnly).Add(_cameraShakeSampler.Sample(oneShotOnly));
            CameraShakePose sum = _cameraShakeSampler.Sample(_cameraShakeModel);
            AssertNear(expected.EulerDegrees, sum.EulerDegrees);
            AssertNear(expected.Offset, sum.Offset);
            Assert.Greater(sum.EulerDegrees.magnitude, 0f);
        }

        [Test]
        public void WhenOneShotOutlivesItsSeconds_ThenItFadesToZero() {
            _cameraShakeService.AddShake(CreateProfile(seconds: 0.35f, meters: 0.1f), 1f);
            AdvanceFrames(10);
            float midway = _cameraShakeModel.OneShots[0].CurrentStrength;

            AdvanceFrames(20);

            Assert.That(midway, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.IsTrue(_cameraShakeModel.OneShots[0].IsOver);
            AssertNear(Vector3.zero, _cameraShakeSampler.Sample(_cameraShakeModel).EulerDegrees);
            AssertNear(Vector3.zero, _cameraShakeSampler.Sample(_cameraShakeModel).Offset);
        }

        [Test]
        public void WhenLengthScalesWithStrength_ThenWeakerShakeEndsSooner() {
            _cameraShakeService.AddShake(CreateProfile(seconds: 0.6f, fadePower: 1f, isLengthScaledByStrength: true), 0.5f);

            AdvanceFrames(19);

            Assert.IsTrue(_cameraShakeModel.OneShots[0].IsOver);
        }

        [Test]
        public void WhenContinuousIntensityIsSet_ThenItEasesThereOverTheProfileSeconds() {
            ICameraShakeHandle handle = StartContinuous(_cameraShakeService, CreateProfile(seconds: 0.5f));
            AdvanceFrames(15);
            float rising = _cameraShakeModel.ContinuousShakes[0].Intensity;
            AdvanceFrames(15);
            float full = _cameraShakeModel.ContinuousShakes[0].Intensity;

            handle.SetIntensity(0f);
            AdvanceFrames(31);

            Assert.AreEqual(0.5f, rising, 0.01f);
            Assert.AreEqual(1f, full, TOLERANCE);
            AssertNear(Vector3.zero, _cameraShakeSampler.Sample(_cameraShakeModel).EulerDegrees);
        }

        [Test]
        public void WhenContinuousShakeIsStopped_ThenItIsRemoved() {
            ICameraShakeHandle handle = StartContinuous(_cameraShakeService, CreateProfile(seconds: 0.1f));
            AdvanceFrames(10);

            handle.Stop();
            AdvanceFrames(1);

            Assert.AreEqual(0, _cameraShakeModel.ContinuousShakes.Count);
        }

        [Test]
        public void WhenModelIsCleared_ThenNoShakeIsLeft() {
            StartContinuous(_cameraShakeService, CreateProfile(seconds: 0.1f));
            _cameraShakeService.AddShake(CreateProfile(seconds: 1f), 1f);
            AdvanceFrames(5);

            _cameraShakeModel.Clear();

            Assert.IsTrue(_cameraShakeModel.IsEmpty);
            AssertNear(Vector3.zero, _cameraShakeSampler.Sample(_cameraShakeModel).EulerDegrees);
        }

        private static ICameraShakeHandle StartContinuous(CameraShakeService cameraShakeService, CameraShakeProfile profile) {
            ICameraShakeHandle handle = cameraShakeService.BeginShake(profile);
            handle.SetIntensity(1f);
            return handle;
        }

        private void AdvanceFrames(int frames) {
            for (int i = 0; i < frames; i++)
                _cameraShakeSampler.Advance(_cameraShakeModel, FRAME_SECONDS);
        }

        private CameraShakeProfile CreateProfile(
            float seconds,
            float frequency = 18f,
            float meters = 0f,
            float fadePower = 2f,
            bool isLengthScaledByStrength = false) {
            CameraShakeProfile profile = ScriptableObject.CreateInstance<CameraShakeProfile>();
            SerializedObject serialized = new(profile);
            serialized.FindProperty("_degrees").floatValue = 2f;
            serialized.FindProperty("_rollShare").floatValue = 0.5f;
            serialized.FindProperty("_meters").floatValue = meters;
            serialized.FindProperty("_seconds").floatValue = seconds;
            serialized.FindProperty("_frequency").floatValue = frequency;
            serialized.FindProperty("_fadePower").floatValue = fadePower;
            serialized.FindProperty("_isLengthScaledByStrength").boolValue = isLengthScaledByStrength;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _profiles.Add(profile);
            return profile;
        }

        private static void AssertNear(Vector3 expected, Vector3 actual) =>
            Assert.Less(Vector3.Distance(expected, actual), TOLERANCE, $"expected {expected}, got {actual}");
    }
}
