using UnityEngine;

namespace Features.WeaponModule.Contracts {
    // A component next to a weapon target's collider that shows a shot striking it (a rock flashes and shakes). The
    // physics ray calls it on every peer that fires or replays the shot, so every player sees it; it changes nothing
    // in the game.
    public interface IWeaponImpactFeedback {
        public void PresentImpact(Vector3 point, Vector3 normal);
    }
}
