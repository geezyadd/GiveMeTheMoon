using System.Collections.Generic;
using Features.CutsceneModule.Contracts;
using Features.CutsceneModule.Scripts.Configuration;
using UnityEngine;

namespace Features.CutsceneModule.Scripts.Core {
    // Each shot eases the camera on a curve bowing up from where the last shot left it to the shot's point, and turns
    // it from the last look onto the shot's focus over the turn seconds; past the last shot it holds the last pose.
    public sealed class CutsceneCameraPath : ICutsceneCameraPath {
        private const float MIN_LOOK_DISTANCE = 0.01f;

        private readonly CutsceneConfiguration _cutsceneConfiguration;

        public CutsceneCameraPath(CutsceneConfiguration cutsceneConfiguration) =>
            _cutsceneConfiguration = cutsceneConfiguration;

        public Pose Evaluate(IReadOnlyList<CutsceneShot> shots, IReadOnlyList<Vector3> focuses, Pose start, float elapsedSeconds) {
            Pose from = start;
            float remaining = elapsedSeconds;
            for (int i = 0; i < shots.Count; i++) {
                CutsceneShot shot = shots[i];
                bool isLast = i == shots.Count - 1;
                if (remaining <= shot.Seconds || isLast)
                    return EvaluateShot(from, shot, focuses[i], Mathf.Min(remaining, shot.Seconds));

                remaining -= shot.Seconds;
                from = new Pose(shot.ViewFrom, LookFrom(shot.ViewFrom, focuses[i], from.rotation));
            }

            return start;
        }

        private Pose EvaluateShot(Pose from, CutsceneShot shot, Vector3 focus, float secondsIntoShot) {
            float share = shot.Seconds > 0f ? Mathf.Clamp01(secondsIntoShot / shot.Seconds) : 1f;
            Vector3 position = AlongCurve(from.position, shot.ViewFrom, Mathf.SmoothStep(0f, 1f, share));
            float turn = Mathf.SmoothStep(0f, 1f, secondsIntoShot / _cutsceneConfiguration.TurnSeconds);
            Quaternion rotation = Quaternion.Slerp(from.rotation, LookFrom(position, focus, from.rotation), turn);
            return new Pose(position, rotation);
        }

        // A quadratic curve whose middle bows up by a share of the distance, so the camera rises as it pulls away.
        private Vector3 AlongCurve(Vector3 from, Vector3 to, float share) {
            Vector3 bow = (from + to) / 2f + Vector3.up * (Vector3.Distance(from, to) * _cutsceneConfiguration.CurveHeightShare);
            Vector3 first = Vector3.Lerp(from, bow, share);
            Vector3 second = Vector3.Lerp(bow, to, share);
            return Vector3.Lerp(first, second, share);
        }

        private static Quaternion LookFrom(Vector3 position, Vector3 focus, Quaternion fallback) {
            Vector3 look = focus - position;
            return look.sqrMagnitude < MIN_LOOK_DISTANCE ? fallback : Quaternion.LookRotation(look, Vector3.up);
        }
    }
}
