using System.Collections.Generic;
using Features.CutsceneModule.Contracts;
using UnityEngine;

namespace Features.CutsceneModule.Scripts.Core {
    public interface ICutsceneCameraPath {
        // Where the cutscene camera stands and looks the given seconds into the shots: start is the player's view
        // the cutscene began from; focuses holds each shot's focus on this peer now (a follow shot's target).
        public Pose Evaluate(IReadOnlyList<CutsceneShot> shots, IReadOnlyList<Vector3> focuses, Pose start, float elapsedSeconds);
    }
}
