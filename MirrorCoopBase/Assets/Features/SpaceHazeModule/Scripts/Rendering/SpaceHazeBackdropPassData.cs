using UnityEngine.Rendering.RenderGraphModule;

namespace Features.SpaceHazeModule.Scripts.Rendering {
    // What the backdrop pass needs when the render graph runs it.
    public sealed class SpaceHazeBackdropPassData {
        public RendererListHandle Things;
        public RendererListHandle Sky;
    }
}
