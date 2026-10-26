using System;
using Features.SpaceHazeModule.Scripts.Configurations;
using Features.SpaceHazeModule.Scripts.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using Zenject;

namespace Features.SpaceHazeModule.Scripts.Rendering {
    // A full-screen pass after the sky: every pixel takes the camera depth, and a thing farther than the clear
    // distance fades into the sky behind it (SpaceHaze.shader). It runs before the transparents and the HUD, so the
    // screen markers are never hazed.
    public sealed class SpaceHazeRenderPass : ScriptableRenderPass, ISpaceHazeRenderer, IInitializable, IDisposable {
        private const string PASS_NAME = "Space Haze";
        private const string COLOR_TEXTURE_NAME = "_SpaceHazeColor";
        private const int SHADER_PASS = 0;

        private static readonly int _distancesId = Shader.PropertyToID("_SpaceHazeDistances");
        private static readonly int _strengthId = Shader.PropertyToID("_SpaceHazeStrength");
        private static readonly int _tintId = Shader.PropertyToID("_SpaceHazeTint");
        private static readonly Vector4 _fullScaleBias = new(1f, 1f, 0f, 0f);

        private readonly SpaceHazeConfiguration _spaceHazeConfiguration;
        private readonly ISpaceHazeSkySource _spaceHazeSkySource;

        private Material _material;

        public SpaceHazeRenderPass(SpaceHazeConfiguration spaceHazeConfiguration, ISpaceHazeSkySource spaceHazeSkySource) {
            _spaceHazeConfiguration = spaceHazeConfiguration;
            _spaceHazeSkySource = spaceHazeSkySource;
        }

        public void Initialize() {
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            profilingSampler = new ProfilingSampler(PASS_NAME);
            ConfigureInput(ScriptableRenderPassInput.Depth);
            // The pass reads the camera colour it writes: URP must not render straight into the back buffer.
            requiresIntermediateTexture = true;
            _material = CoreUtils.CreateEngineMaterial(_spaceHazeConfiguration.Shader);
        }

        public void Dispose() =>
            CoreUtils.Destroy(_material);

        public void Enqueue(ScriptableRenderer renderer, SpaceHazeShape shape) {
            _material.SetVector(_distancesId, new Vector4(shape.ClearMeters, shape.HiddenMeters, shape.BackdropStartMeters, shape.BackdropEndMeters));
            _material.SetFloat(_strengthId, shape.Strength);
            _material.SetColor(_tintId, _spaceHazeConfiguration.Tint);
            _spaceHazeSkySource.Apply(_material);
            renderer.EnqueuePass(this);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;

            TextureHandle source = resourceData.activeColorTexture;
            TextureDesc destinationDescription = renderGraph.GetTextureDesc(source);
            destinationDescription.name = COLOR_TEXTURE_NAME;
            destinationDescription.clearBuffer = false;
            TextureHandle destination = renderGraph.CreateTexture(destinationDescription);
            using (IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(PASS_NAME, out SpaceHazePassData passData, profilingSampler)) {
                passData.Source = source;
                passData.Material = _material;
                builder.UseTexture(source);
                builder.UseTexture(resourceData.cameraDepthTexture);
                builder.SetRenderAttachment(destination, 0);
                builder.SetRenderFunc<SpaceHazePassData>(ExecutePass);
            }

            resourceData.cameraColor = destination;
        }

        private static void ExecutePass(SpaceHazePassData passData, RasterGraphContext context) =>
            Blitter.BlitTexture(context.cmd, passData.Source, _fullScaleBias, passData.Material, SHADER_PASS);
    }
}
