using Features.ConstellationModule.Scripts.Configurations;
using Features.ConstellationModule.Scripts.Core;
using Features.ConstellationModule.Scripts.Definitions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Features.ConstellationModule.Editor.Tests {
    // Four stars on the celestial equator 10 deg apart (RA 0, 10, 20 and 40); lines 0-1, 1-2 and 2-0 (closing the loop
    // on a lit star, 20 deg long); star 3 is on no line. Fade-in 0.5 s, pause 0.1 s, 10 deg/s, so the steps are:
    // star 0 at 0, line 0 at 0.6..1.6, star 1 at 1.6, line 1 at 2.2..3.2, star 2 at 3.2, line 2 at 3.8..5.8, star 3 at 5.9.
    public sealed class ConstellationRevealTimelineTests {
        private const float STAR_FADE_IN_SECONDS = 0.5f;
        private const float PAUSE_SECONDS = 0.1f;
        private const float LINE_DEGREES_PER_SECOND = 10f;
        private const float TOLERANCE = 0.001f;

        private ConstellationHighlightConfiguration _configuration;
        private ConstellationDefinition _constellation;

        [SetUp]
        public void SetUp() {
            _configuration = ScriptableObject.CreateInstance<ConstellationHighlightConfiguration>();
            SetUpConfiguration(flashOvershoot: 0f);
            _constellation = ScriptableObject.CreateInstance<ConstellationDefinition>();
            SerializedObject constellation = new SerializedObject(_constellation);
            SerializedProperty stars = constellation.FindProperty("_stars");
            double[] rightAscensions = { 0.0, 10.0, 20.0, 40.0 };
            stars.arraySize = rightAscensions.Length;
            for (int i = 0; i < rightAscensions.Length; i++)
                stars.GetArrayElementAtIndex(i).FindPropertyRelative("_rightAscensionDegrees").doubleValue = rightAscensions[i];
            SerializedProperty lines = constellation.FindProperty("_lines");
            int[,] ends = { { 0, 1 }, { 1, 2 }, { 2, 0 } };
            lines.arraySize = ends.GetLength(0);
            for (int i = 0; i < ends.GetLength(0); i++) {
                lines.GetArrayElementAtIndex(i).FindPropertyRelative("_fromStar").intValue = ends[i, 0];
                lines.GetArrayElementAtIndex(i).FindPropertyRelative("_toStar").intValue = ends[i, 1];
            }
            constellation.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown() {
            Object.DestroyImmediate(_configuration);
            Object.DestroyImmediate(_constellation);
        }

        [Test]
        public void WhenTheRevealStarts_ThenOnlyTheFirstLinesFirstStarLightsUp() {
            ConstellationRevealTimeline timeline = CreateTimeline();

            Assert.AreEqual(0.5f, timeline.GetStarLight(0, 0.25f), TOLERANCE);
            Assert.AreEqual(0f, timeline.GetStarLight(1, 0.25f), TOLERANCE);
            Assert.AreEqual(0f, timeline.GetLineProgress(0, 0.25f), TOLERANCE);
        }

        [Test]
        public void WhenTheFirstStarHasLitAndPaused_ThenTheFirstLineGrowsTowardItsSecondStar() {
            ConstellationRevealTimeline timeline = CreateTimeline();

            Assert.AreEqual(1f, timeline.GetStarLight(0, 1.1f), TOLERANCE);
            Assert.AreEqual(0.5f, timeline.GetLineProgress(0, 1.1f), TOLERANCE);
            Assert.AreEqual(0f, timeline.GetStarLight(1, 1.1f), TOLERANCE);
        }

        [Test]
        public void WhenALineReachesItsSecondStar_ThenThatStarLightsUp() {
            ConstellationRevealTimeline timeline = CreateTimeline();

            Assert.AreEqual(1f, timeline.GetLineProgress(0, 1.85f), TOLERANCE);
            Assert.AreEqual(0.5f, timeline.GetStarLight(1, 1.85f), TOLERANCE);
            Assert.AreEqual(0f, timeline.GetLineProgress(1, 1.85f), TOLERANCE);
        }

        [Test]
        public void WhenALineStartsAtALitStar_ThenItStartsRightAfterThePreviousStep() {
            ConstellationRevealTimeline timeline = CreateTimeline();

            Assert.AreEqual(0f, timeline.GetLineProgress(1, 2.2f), TOLERANCE);
            Assert.AreEqual(0.5f, timeline.GetLineProgress(1, 2.7f), TOLERANCE);
        }

        [Test]
        public void WhenALineIsLonger_ThenItTakesLongerToDraw() {
            ConstellationRevealTimeline timeline = CreateTimeline();

            Assert.AreEqual(0.5f, timeline.GetLineProgress(2, 4.8f), TOLERANCE);
            Assert.AreEqual(1f, timeline.GetLineProgress(2, 5.8f), TOLERANCE);
        }

        [Test]
        public void WhenALineClosesOnALitStar_ThenThatStarStaysLit() {
            ConstellationRevealTimeline timeline = CreateTimeline();

            Assert.AreEqual(1f, timeline.GetStarLight(0, 5.85f), TOLERANCE);
        }

        [Test]
        public void WhenAStarIsOnNoLine_ThenItLightsUpAfterTheLastLineAndPause() {
            ConstellationRevealTimeline timeline = CreateTimeline();

            Assert.AreEqual(0f, timeline.GetStarLight(3, 5.85f), TOLERANCE);
            Assert.AreEqual(0.5f, timeline.GetStarLight(3, 6.15f), TOLERANCE);
        }

        [Test]
        public void WhenAStarFlashes_ThenItPassesFullLightOnTheWayAndEndsExactlyAtIt() {
            SetUpConfiguration(flashOvershoot: 0.5f);
            ConstellationRevealTimeline timeline = CreateTimeline();

            Assert.Greater(timeline.GetStarLight(0, 0.3f), 1f);
            Assert.AreEqual(1f, timeline.GetStarLight(0, STAR_FADE_IN_SECONDS), TOLERANCE);
            Assert.AreEqual(1f, timeline.GetStarLight(0, 100f), TOLERANCE);
        }

        [Test]
        public void WhenTheRevealWaitsBelowZero_ThenNothingIsLit() {
            ConstellationRevealTimeline timeline = CreateTimeline();

            Assert.AreEqual(0f, timeline.GetStarLight(0, -0.3f), TOLERANCE);
            Assert.AreEqual(0f, timeline.GetLineProgress(0, -0.3f), TOLERANCE);
        }

        private ConstellationRevealTimeline CreateTimeline() =>
            new ConstellationRevealTimelineFactory(_configuration).Create(_constellation);

        private void SetUpConfiguration(float flashOvershoot) {
            SerializedObject configuration = new SerializedObject(_configuration);
            configuration.FindProperty("_starFadeInSeconds").floatValue = STAR_FADE_IN_SECONDS;
            configuration.FindProperty("_starFlashOvershoot").floatValue = flashOvershoot;
            configuration.FindProperty("_lineDrawDegreesPerSecond").floatValue = LINE_DEGREES_PER_SECOND;
            configuration.FindProperty("_pauseBetweenStepsSeconds").floatValue = PAUSE_SECONDS;
            configuration.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
