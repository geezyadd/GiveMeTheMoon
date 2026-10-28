using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Features.SpaceHazeModule.Scripts.Rendering {
    // The backdrop a hazed thing fades into: the sky and every opaque thing that lies wholly past the backdrop
    // distance (the planets, a far station), drawn again for the camera into a texture of its own. The things are
    // picked by a culling of their own whose near plane stands at the backdrop distance, so no layer or tag marks
    // them and nothing nearer (rocks, ore, the ship) gets in, whatever is added to the field later.
    public sealed class SpaceHazeBackdropRecorder : ISpaceHazeBackdropRecorder {
        private const string PASS_NAME = "Space Haze Backdrop";
        private const string COLOR_TEXTURE_NAME = "_SpaceHazeBackdrop";
        private const string DEPTH_TEXTURE_NAME = "_SpaceHazeBackdropDepth";
        // The culling planes go left, right, bottom, top, near, far.
        private const int NEAR_PLANE_INDEX = 4;
        private const int NO_PER_OBJECT_LIGHT = -1;

        private static readonly List<ShaderTagId> _shaderTagIds = new() {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
        };

        private readonly ProfilingSampler _profilingSampler = new(PASS_NAME);

        private CullingResults _cullingResults;

        public bool TryCull(ScriptableRenderContext context, Camera camera, float backdropMeters) {
            if (camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters) == false)
                return false;

            Transform cameraTransform = camera.transform;
            Vector3 forward = cameraTransform.forward;
            cullingParameters.SetCullingPlane(NEAR_PLANE_INDEX, new Plane(forward, cameraTransform.position + forward * backdropMeters));
            cullingParameters.cullingOptions &= ~(CullingOptions.ShadowCasters | CullingOptions.OcclusionCull);
            cullingParameters.shadowDistance = 0f;
            _cullingResults = context.Cull(ref cullingParameters);
            ClearPerObjectLights();
            return true;
        }

        public TextureHandle Record(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            TextureHandle color = CreateTexture(renderGraph, resourceData.activeColorTexture, COLOR_TEXTURE_NAME);
            TextureHandle depth = CreateTexture(renderGraph, resourceData.activeDepthTexture, DEPTH_TEXTURE_NAME);
            DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(_shaderTagIds, frameData.Get<UniversalRenderingData>(), cameraData, frameData.Get<UniversalLightData>(), SortingCriteria.CommonOpaque);
            FilteringSettings filteringSettings = new(RenderQueueRange.opaque, cameraData.camera.cullingMask);
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(PASS_NAME, out SpaceHazeBackdropPassData passData, _profilingSampler)) {
                passData.Things = renderGraph.CreateRendererList(new RendererListParams(_cullingResults, drawingSettings, filteringSettings));
                passData.Sky = renderGraph.CreateSkyboxRendererList(cameraData.camera);
                builder.UseRendererList(passData.Things);
                builder.UseRendererList(passData.Sky);
                builder.SetRenderAttachment(color, 0);
                builder.SetRenderAttachmentDepth(depth);
                builder.SetRenderFunc<SpaceHazeBackdropPassData>(ExecutePass);
            }

            return color;
        }

        // The backdrop's own culling numbers the lights apart from the camera's, so per-object light lists would
        // point at the wrong lights of the forward renderer; a backdrop thing keeps the main light only.
        private void ClearPerObjectLights() {
            NativeArray<int> lightIndexMap = _cullingResults.GetLightIndexMap(Allocator.Temp);
            for (int i = 0; i < lightIndexMap.Length; i++)
                lightIndexMap[i] = NO_PER_OBJECT_LIGHT;

            _cullingResults.SetLightIndexMap(lightIndexMap);
            lightIndexMap.Dispose();
        }

        private static TextureHandle CreateTexture(RenderGraph renderGraph, TextureHandle cameraTexture, string name) {
            TextureDesc description = renderGraph.GetTextureDesc(cameraTexture);
            description.name = name;
            description.msaaSamples = MSAASamples.None;
            description.bindTextureMS = false;
            description.clearBuffer = false;
            return renderGraph.CreateTexture(description);
        }

        private static void ExecutePass(SpaceHazeBackdropPassData passData, RasterGraphContext context) {
            context.cmd.ClearRenderTarget(true, true, Color.clear);
            context.cmd.DrawRendererList(passData.Things);
            context.cmd.DrawRendererList(passData.Sky);
        }
    }
}
