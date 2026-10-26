using UnityEngine;

namespace Features.SpaceHazeModule.Scripts.Rendering {
    public interface ISpaceHazeSkySource {
        // Gives the haze material the sky a hazed thing fades into.
        public void Apply(Material hazeMaterial);
    }
}
