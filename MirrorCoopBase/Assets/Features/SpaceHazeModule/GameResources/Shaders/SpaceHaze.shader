// The space haze (SpaceHazeRenderPass): a full-screen pass that fades every pixel farther than the clear distance
// into the backdrop behind it (the sky and the planets). The haze share is SpaceHazeRule.GetHazeShare: change both
// together.
Shader "Hidden/SpaceHaze/SpaceHaze" {
    SubShader {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off
        Blend Off

        Pass {
            Name "SpaceHaze"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // x: clear, y: hidden, z: backdrop start, w: backdrop end, in metres from the camera.
            float4 _SpaceHazeDistances;
            float _SpaceHazeStrength;
            // rgb: the haze's own colour, a: how much of it lies over the backdrop.
            float4 _SpaceHazeTint;
            // What lies behind a hazed thing: the sky and the backdrop things (SpaceHazeBackdropRecorder).
            TEXTURE2D_X(_SpaceHazeBackdrop);

            float GetHazeShare(float distance) {
                float fadeIn = smoothstep(_SpaceHazeDistances.x, _SpaceHazeDistances.y, distance);
                float fadeOut = smoothstep(_SpaceHazeDistances.z, _SpaceHazeDistances.w, distance);
                return fadeIn * (1.0 - fadeOut) * _SpaceHazeStrength;
            }

            half4 Frag(Varyings input) : SV_Target {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
                float depth = SampleSceneDepth(uv);
                #if !UNITY_REVERSED_Z
                depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, depth);
                #endif
                float3 toPixel = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP) - _WorldSpaceCameraPos;
                float distance = length(toPixel);
                float share = GetHazeShare(distance);
                if (share <= 0.0)
                    return color;

                float3 backdrop = SAMPLE_TEXTURE2D_X(_SpaceHazeBackdrop, sampler_PointClamp, uv).rgb;
                float3 haze = lerp(backdrop, _SpaceHazeTint.rgb, _SpaceHazeTint.a);

                color.rgb = lerp(color.rgb, haze, share);
                return color;
            }
            ENDHLSL
        }
    }
}
