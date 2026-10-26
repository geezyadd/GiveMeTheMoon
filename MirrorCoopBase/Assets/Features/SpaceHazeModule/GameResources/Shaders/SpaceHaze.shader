// The space haze (SpaceHazeRenderPass): a full-screen pass that fades every pixel farther than the clear distance
// into the sky behind it. The haze share is SpaceHazeRule.GetHazeShare: change both together.
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
            // rgb: the haze's own colour, a: how much of it lies over the sky.
            float4 _SpaceHazeTint;
            float _SpaceHazeHasSky;
            float4 _SpaceHazeSkyColor;
            float _SpaceHazeSkyRotation;

            // The faces of the scene's six-sided skybox (Skybox/6 Sided): Front +Z, Back -Z, Left +X, Right -X.
            TEXTURE2D(_FrontTex);
            TEXTURE2D(_BackTex);
            TEXTURE2D(_LeftTex);
            TEXTURE2D(_RightTex);
            TEXTURE2D(_UpTex);
            TEXTURE2D(_DownTex);

            // The skybox turns its cube by _Rotation around Y, so the sky seen along a direction lies on the cube
            // turned back by that angle.
            float3 RotateToSky(float3 direction) {
                float angle = -_SpaceHazeSkyRotation * PI / 180.0;
                float sine = sin(angle);
                float cosine = cos(angle);
                float2 turned = mul(float2x2(cosine, -sine, sine, cosine), direction.xz);
                return float3(turned.x, direction.y, turned.y);
            }

            float3 SampleSky(float3 direction) {
                float3 cube = RotateToSky(direction);
                float3 size = abs(cube);
                float3 sky;
                if (size.z >= size.x && size.z >= size.y) {
                    float2 face = cube.xy / size.z;
                    sky = cube.z > 0
                        ? SAMPLE_TEXTURE2D_LOD(_FrontTex, sampler_LinearClamp, float2(face.x, face.y) * 0.5 + 0.5, 0).rgb
                        : SAMPLE_TEXTURE2D_LOD(_BackTex, sampler_LinearClamp, float2(-face.x, face.y) * 0.5 + 0.5, 0).rgb;
                } else if (size.x >= size.y) {
                    float2 face = cube.zy / size.x;
                    sky = cube.x > 0
                        ? SAMPLE_TEXTURE2D_LOD(_LeftTex, sampler_LinearClamp, float2(-face.x, face.y) * 0.5 + 0.5, 0).rgb
                        : SAMPLE_TEXTURE2D_LOD(_RightTex, sampler_LinearClamp, float2(face.x, face.y) * 0.5 + 0.5, 0).rgb;
                } else {
                    float2 face = cube.xz / size.y;
                    sky = cube.y > 0
                        ? SAMPLE_TEXTURE2D_LOD(_UpTex, sampler_LinearClamp, float2(face.x, -face.y) * 0.5 + 0.5, 0).rgb
                        : SAMPLE_TEXTURE2D_LOD(_DownTex, sampler_LinearClamp, float2(face.x, face.y) * 0.5 + 0.5, 0).rgb;
                }

                return sky * _SpaceHazeSkyColor.rgb;
            }

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

                float3 haze = _SpaceHazeTint.rgb;
                if (_SpaceHazeHasSky > 0.5)
                    haze = lerp(SampleSky(toPixel / distance), _SpaceHazeTint.rgb, _SpaceHazeTint.a);

                color.rgb = lerp(color.rgb, haze, share);
                return color;
            }
            ENDHLSL
        }
    }
}
