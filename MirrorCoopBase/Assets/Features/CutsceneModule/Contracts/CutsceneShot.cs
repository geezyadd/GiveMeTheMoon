using System;
using UnityEngine;

namespace Features.CutsceneModule.Contracts {
    // One shot of a cutscene, the same for every player: over Seconds the camera moves on a curve from where the last
    // shot left it (the player's view for the first) to ViewFrom and turns to look at Focus, or at the target named by
    // TargetId while one is found on that peer (Focus otherwise). World points: a cutscene lasts seconds, no world
    // shift happens in flight.
    [Serializable]
    public struct CutsceneShot {
        public CutsceneShotKind Kind;
        public Vector3 ViewFrom;
        public Vector3 Focus;
        public string TargetId;
        public float Seconds;

        public static CutsceneShot LookAt(Vector3 viewFrom, Vector3 focus, float seconds) =>
            new() { Kind = CutsceneShotKind.LookAtPoint, ViewFrom = viewFrom, Focus = focus, TargetId = string.Empty, Seconds = seconds };

        public static CutsceneShot Follow(Vector3 viewFrom, string targetId, Vector3 fallbackFocus, float seconds) =>
            new() { Kind = CutsceneShotKind.FollowTarget, ViewFrom = viewFrom, Focus = fallbackFocus, TargetId = targetId, Seconds = seconds };
    }
}
