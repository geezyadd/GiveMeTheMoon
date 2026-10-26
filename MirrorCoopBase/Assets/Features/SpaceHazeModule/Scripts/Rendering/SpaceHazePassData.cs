using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

namespace Features.SpaceHazeModule.Scripts.Rendering {
    // What the haze pass needs when the render graph runs it.
    public sealed class SpaceHazePassData {
        public TextureHandle Source;
        public TextureHandle Backdrop;
        public Material Material;
    }
}
