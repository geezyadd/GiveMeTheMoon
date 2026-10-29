using System.Collections.Generic;
using Features.SonarModule.Contracts;
using UnityEngine;

namespace Features.SonarModule.Scripts.Sources {
    public interface IOverboardSonarContactSource {
        public SonarContactKind Kind { get; }

        // Where every player drifting overboard but this peer's own is, for the screen's markers; the list is not cleared.
        public void CollectOtherDrifterPositions(List<Vector3> results);
    }
}
