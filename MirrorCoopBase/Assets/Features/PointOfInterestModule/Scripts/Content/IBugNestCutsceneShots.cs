using System.Collections.Generic;
using Features.CutsceneModule.Contracts;
using UnityEngine;

namespace Features.PointOfInterestModule.Scripts.Content {
    public interface IBugNestCutsceneShots {
        // The nest's entry cutscene (the list is cleared first): the camera pulls away from the ship and looks at the
        // nest while the bugs come out, then follows the middle of the flock.
        public void Create(Vector3 nest, Vector3 shipPosition, string flockTargetId, List<CutsceneShot> shots);
    }
}
