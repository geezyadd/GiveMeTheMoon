using System.Collections.Generic;
using Features.CutsceneModule.Contracts;
using Features.PointOfInterestModule.Scripts.Configuration;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Content {
    // The camera first stands behind and above the ship, seen from the nest, so the ship and the nest are in the frame;
    // for the follow shot it flies part of the way toward the nest and to the side, so the flock passes close by.
    public sealed class BugNestCutsceneShots : IBugNestCutsceneShots {
        private const float DIRECTION_EPSILON = 0.0001f;

        private readonly BugNestConfiguration _bugNestConfiguration;

        public BugNestCutsceneShots(BugNestConfiguration bugNestConfiguration) =>
            _bugNestConfiguration = bugNestConfiguration;

        public void Create(Vector3 nest, Vector3 shipPosition, string flockTargetId, List<CutsceneShot> shots) {
            shots.Clear();
            Vector3 towardNest = FlatDirection(nest - shipPosition);
            Vector3 side = Vector3.Cross(Vector3.up, towardNest);
            Vector3 pulledAway = shipPosition - towardNest * _bugNestConfiguration.CameraPullBackMeters + Vector3.up * _bugNestConfiguration.CameraPullUpMeters;
            Vector3 towardShip = Vector3.Lerp(nest, shipPosition, _bugNestConfiguration.CameraFollowNestShare);
            Vector3 drifted = towardShip + side * _bugNestConfiguration.CameraFollowSideMeters + Vector3.up * _bugNestConfiguration.CameraPullUpMeters;
            shots.Add(CutsceneShot.LookAt(pulledAway, nest, _bugNestConfiguration.LookAtNestSeconds));
            shots.Add(CutsceneShot.Follow(drifted, flockTargetId, nest, _bugNestConfiguration.FollowFlockSeconds));
        }

        private static Vector3 FlatDirection(Vector3 direction) {
            direction.y = 0f;
            return direction.sqrMagnitude < DIRECTION_EPSILON ? Vector3.forward : direction.normalized;
        }
    }
}
