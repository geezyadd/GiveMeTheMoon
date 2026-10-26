using Features.SpaceHazeModule.Scripts.Core;
using UnityEngine.Rendering.Universal;

namespace Features.SpaceHazeModule.Scripts.Rendering {
    public interface ISpaceHazeRenderer {
        // Draws the haze in the camera this renderer renders now; asked again for every camera every frame.
        public void Enqueue(ScriptableRenderer renderer, SpaceHazeShape shape);
    }
}
