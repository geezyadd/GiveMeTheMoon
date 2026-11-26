using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Features.ScreenShakeModule.Editor.Tests {
    // Only the strongest running shake is shown; each one runs out its own time, so a long weak one goes on after a
    // short strong one; with none left the cameras stop shaking.
    public sealed class ScreenShakeSystemTests {
        private readonly List<ScreenShakeProfile> _profiles = new();

        [TearDown]
        public void TearDown() {
            foreach (ScreenShakeProfile profile in _profiles)
                Object.DestroyImmediate(profile);

            _profiles.Clear();
        }

        [Test]
        public void WhenTwoShakesRun_ThenOnlyTheStrongestIsShown() {
            ScreenShakeModel model = new ScreenShakeModel();
            RecordingShaker shaker = new RecordingShaker();
            model.AddShaker(shaker);
            ScreenShakeService service = new ScreenShakeService(model);

            service.Shake(CreateProfile(1f, 2f, 1f));
            service.Shake(CreateProfile(0.3f, 9f, 1f));
            Step(model, 0.1f);

            Assert.AreEqual(0.9f, shaker.Amplitude, 0.01f);
            Assert.AreEqual(2f, shaker.Frequency);
        }

        [Test]
        public void WhenTheStrongShakeEnds_ThenTheWeakerLongerOneGoesOnWithItsOwnFade() {
            ScreenShakeModel model = new ScreenShakeModel();
            RecordingShaker shaker = new RecordingShaker();
            model.AddShaker(shaker);
            ScreenShakeService service = new ScreenShakeService(model);

            service.Shake(CreateProfile(1f, 2f, 0.2f));
            service.Shake(CreateProfile(0.4f, 9f, 1f));
            Step(model, 0.5f);

            Assert.AreEqual(0.2f, shaker.Amplitude, 0.01f);
            Assert.AreEqual(9f, shaker.Frequency);
        }

        [Test]
        public void WhenEveryShakeEnded_ThenTheCamerasStop() {
            ScreenShakeModel model = new ScreenShakeModel();
            RecordingShaker shaker = new RecordingShaker();
            model.AddShaker(shaker);
            new ScreenShakeService(model).Shake(CreateProfile(1f, 2f, 0.2f));

            Step(model, 0.3f);

            Assert.AreEqual(0f, shaker.Amplitude);
            Assert.IsEmpty(model.Shakes);
        }

        private ScreenShakeProfile CreateProfile(float amplitude, float frequency, float duration) {
            ScreenShakeProfile profile = ScriptableObject.CreateInstance<ScreenShakeProfile>();
            var serialized = new UnityEditor.SerializedObject(profile);
            serialized.FindProperty("_amplitude").floatValue = amplitude;
            serialized.FindProperty("_frequency").floatValue = frequency;
            serialized.FindProperty("_duration").floatValue = duration;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _profiles.Add(profile);
            return profile;
        }

        private static void Step(ScreenShakeModel model, float seconds) =>
            new ScreenShakeSystem(model).Advance(seconds);

        private sealed class RecordingShaker : ICameraShaker {
            public float Amplitude { get; private set; }
            public float Frequency { get; private set; }

            public void Apply(float amplitude, float frequency) {
                Amplitude = amplitude;
                Frequency = frequency;
            }
        }
    }
}
