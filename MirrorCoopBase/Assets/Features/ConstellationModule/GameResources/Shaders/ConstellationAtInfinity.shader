// A constellation highlight (ConstellationHighlightView) drawn at infinity, like the skybox: every vertex is a world
// direction, the camera's position is ignored, and the depth sits on the far plane, so the figure never slides
// against the painted stars and any world geometry hides it. Unlit, unfogged, added onto the sky.
Shader "Features/ConstellationModule/ConstellationAtInfinity" {
    Properties {
        [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
    }
    SubShader {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        ZWrite Off
        ZTest LEqual
        Cull Off
        Blend One One

        Pass {
            Name "ConstellationAtInfinity"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Just inside the far plane: behind every opaque thing, in front of the cleared depth.
            #if UNITY_REVERSED_Z
                #define FAR_PLANE_DEPTH 0.00001
            #else
                #define FAR_PLANE_DEPTH 0.99999
            #endif

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes {
                float3 direction : POSITION;
                // A star quad's corner in -1..1; zero on the figure's lines.
                float2 corner : TEXCOORD0;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 corner : TEXCOORD0;
            };

            Varyings Vert(Attributes input) {
                Varyings output;
                output.positionCS = mul(UNITY_MATRIX_VP, float4(input.direction, 0.0));
                output.positionCS.z = output.positionCS.w * FAR_PLANE_DEPTH;
                output.corner = input.corner;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target {
                half glow = saturate(1.0 - length(input.corner));
                half glowSquared = glow * glow;
                return _Color * (glowSquared * glowSquared);
            }
            ENDHLSL
        }
    }
}
