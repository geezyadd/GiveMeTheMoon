using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Features.SpaceHazeModule.Scripts.Rendering {
    public interface ISpaceHazeBackdropRecorder {
        // Picks for this camera's frame the things that lie wholly past the given distance; false when the camera
        // cannot be culled now.
        public bool TryCull(ScriptableRenderContext context, Camera camera, float backdropMeters);

        // Draws the sky and the picked things into a texture of the camera's size and returns it.
        public TextureHandle Record(RenderGraph renderGraph, ContextContainer frameData);
    }
}
