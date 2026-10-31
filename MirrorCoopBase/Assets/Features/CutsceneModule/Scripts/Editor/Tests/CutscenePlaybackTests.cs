using System.Collections.Generic;
using Features.AddressablesConstantGeneratorModule.EditorLoading;
using Features.AddressablesConstantsGenerator.Generated;
using Features.CutsceneModule.Contracts;
using Features.CutsceneModule.Scripts.Configuration;
using Features.CutsceneModule.Scripts.Core;
using Features.CutsceneModule.Scripts.Network;
using NUnit.Framework;
using UnityEngine;

namespace Features.CutsceneModule.Scripts.Editor.Tests {
    // A peer plays a playback once from its start time, ends it at the end of its shots, on a skip or when another
    // one replaced it; the camera starts at the player's view, ends a shot at its point looking at its focus and
    // follows a moving focus.
    public sealed class CutscenePlaybackTests {
        private const double START = 100.0;
        private const float SHOT_SECONDS = 2f;
        private const float POSITION_TOLERANCE = 0.01f;
        private const float ANGLE_TOLERANCE = 0.5f;

        private readonly ICutscenePlaybackRule _rule = new CutscenePlaybackRule();

        private ICutsceneCameraPath _path;

        [SetUp]
        public void SetUp() =>
            _path = new CutsceneCameraPath(AddressableConfigurationLoader.Load<CutsceneConfiguration>(Address.Configurations.CutsceneConfiguration_Default));

        [Test]
        public void WhenAPlaybackRuns_ThenAPeerStartsItOnceAndEndsItAtTheEndOfItsShots() {
            CutscenePlayback playback = CreatePlayback(1);

            Assert.IsTrue(_rule.ShouldStart(playback, 0, START + 0.5));
            Assert.IsFalse(_rule.ShouldStart(playback, 1, START + 0.5), "The same sequence started twice.");
            Assert.IsFalse(_rule.ShouldEnd(playback, 1, START + 0.5));
            Assert.IsTrue(_rule.ShouldEnd(playback, 1, START + SHOT_SECONDS * 2 + 0.1));
            Assert.IsFalse(_rule.ShouldStart(playback, 0, START + SHOT_SECONDS * 2 + 0.1), "A playback that is over started.");
        }

        [Test]
        public void WhenAPlaybackIsSkippedOrReplaced_ThenThePeerEndsIt() {
            CutscenePlayback skipped = CreatePlayback(1);
            skipped.IsSkipped = true;

            Assert.IsTrue(_rule.ShouldEnd(skipped, 1, START + 0.5));
            Assert.IsTrue(_rule.ShouldEnd(CreatePlayback(2), 1, START + 0.5));
            Assert.IsFalse(_rule.ShouldStart(skipped, 0, START + 0.5));
        }

        [Test]
        public void WhenTheShotsPlay_ThenTheCameraStartsAtTheViewAndEndsAtTheShotLookingAtItsFocus() {
            CutscenePlayback playback = CreatePlayback(1);
            Pose start = new(Vector3.zero, Quaternion.identity);
            List<Vector3> focuses = new() { playback.Shots[0].Focus, playback.Shots[1].Focus };

            Pose atStart = _path.Evaluate(playback.Shots, focuses, start, 0f);
            Pose afterFirst = _path.Evaluate(playback.Shots, focuses, start, SHOT_SECONDS);

            Assert.Less(Vector3.Distance(atStart.position, start.position), POSITION_TOLERANCE);
            Assert.Less(Quaternion.Angle(atStart.rotation, start.rotation), ANGLE_TOLERANCE);
            Assert.Less(Vector3.Distance(afterFirst.position, playback.Shots[0].ViewFrom), POSITION_TOLERANCE);
            Assert.Less(Vector3.Angle(afterFirst.rotation * Vector3.forward, playback.Shots[0].Focus - afterFirst.position), ANGLE_TOLERANCE);
        }

        [Test]
        public void WhenAFollowedTargetMoves_ThenTheCameraLooksAtItsNewPlace() {
            CutscenePlayback playback = CreatePlayback(1);
            Pose start = new(Vector3.zero, Quaternion.identity);
            Vector3 movedTarget = playback.Shots[1].Focus + Vector3.right * 30f;
            List<Vector3> focuses = new() { playback.Shots[0].Focus, movedTarget };

            Pose atEnd = _path.Evaluate(playback.Shots, focuses, start, SHOT_SECONDS * 2);

            Assert.Less(Vector3.Angle(atEnd.rotation * Vector3.forward, movedTarget - atEnd.position), ANGLE_TOLERANCE);
        }

        private static CutscenePlayback CreatePlayback(int sequence) =>
            new() {
                Sequence = sequence,
                StartTime = START,
                ShotsSeconds = SHOT_SECONDS * 2,
                Shots = new[] {
                    CutsceneShot.LookAt(new Vector3(0f, 20f, -30f), new Vector3(0f, 0f, 100f), SHOT_SECONDS),
                    CutsceneShot.Follow(new Vector3(10f, 20f, -30f), "target", new Vector3(0f, 0f, 100f), SHOT_SECONDS),
                },
            };
    }
}
