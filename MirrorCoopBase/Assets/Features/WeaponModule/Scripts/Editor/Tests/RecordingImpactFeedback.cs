using Features.WeaponModule.Contracts;
using UnityEngine;

namespace Features.WeaponModule.Editor.Tests {
    // Counts the strikes a surface was asked to show.
    public sealed class RecordingImpactFeedback : IWeaponImpactFeedback {
        public int Impacts { get; private set; }

        public void PresentImpact(Vector3 point, Vector3 normal) =>
            Impacts++;
    }
}
