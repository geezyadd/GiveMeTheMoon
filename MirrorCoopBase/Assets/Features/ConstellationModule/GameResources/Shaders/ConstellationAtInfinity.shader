// A constellation highlight (ConstellationHighlightView) drawn at infinity, like the skybox: every vertex is a world
// direction, the camera's position is ignored, and the depth sits on the far plane, so the figure never slides
// against the painted stars and any world geometry hides it. Unlit, unfogged, added onto the sky.
// One shader for both submeshes (ConstellationMeshBuilder): _IsLine picks the star look or the line look.
Shader "Features/ConstellationModule/ConstellationAtInfinity" {
    Properties {
        [HDR] _Color ("Color", Color) = (1, 1, 1, 1)
        [MaterialToggle] _IsLine ("Is Line", Float) = 0
        _CoreRadius ("Core Radius (deg)", Float) = 0.04
        _CoreIntensity ("Core Intensity", Float) = 8
        _HaloRadius ("Halo Radius (deg)", Float) = 0.55
        _HaloIntensity ("Halo Intensity", Float) = 0.3
        _SpikeLength ("Spike Length (deg)", Float) = 1.4
        _SpikeWidth ("Spike Width (deg)", Float) = 0.015
        _SpikeIntensity ("Spike Intensity", Float) = 1.6
        _TwinkleAmount ("Twinkle Amount", Float) = 0
        _TwinkleSpeed ("Twinkle Speed", Float) = 0.7
        _LineWidth ("Line Width (px)", Float) = 8
        _LineIntensity ("Line Intensity", Float) = 1.3
        _LineGlowIntensity ("Line Glow Intensity", Float) = 0.18
        _LineEndFade ("Line End Fade (deg)", Float) = 0.25
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
            // Across its strip a line is a thin Gaussian core (a fifth of the strip) over a wide Gaussian glow that is all but
            // zero (e^-4.8) at the strip's edges.
            #define LINE_CORE_SHARPNESS 5.0
            #define LINE_GLOW_SHARPNESS 2.2
            #define MIN_CLIP_W 0.0001

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _IsLine;
                float _CoreRadius;
                float _CoreIntensity;
                float _HaloRadius;
                float _HaloIntensity;
                float _SpikeLength;
                float _SpikeWidth;
                float _SpikeIntensity;
                float _TwinkleAmount;
                float _TwinkleSpeed;
                float _LineWidth;
                float _LineIntensity;
                float _LineGlowIntensity;
                float _LineEndFade;
            CBUFFER_END

            struct Attributes {
                float3 direction : POSITION;
                // Star: shape position in degrees, brightness, twinkle phase. Line: side, degrees from the start,
                // length in degrees, +1 at the start / -1 at the end (see ConstellationMeshBuilder).
                float4 shape : TEXCOORD0;
                // Line: direction of the line's other end. Unused by stars.
                float3 otherEnd : TEXCOORD1;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float4 shape : TEXCOORD0;
            };

            float4 ProjectAtInfinity(float3 direction) {
                return mul(UNITY_MATRIX_VP, float4(direction, 0.0));
            }

            // Moves a line vertex sideways on screen by half the line's width, so the strip has the same width in
            // pixels at any zoom. A line with an end behind the camera is collapsed: Orion's lines are a few degrees
            // long, so that only happens far outside the view.
            float4 WidenLine(float4 positionCS, float3 otherEnd, float side, float lineEnd) {
                float4 otherCS = ProjectAtInfinity(otherEnd);
                if (positionCS.w < MIN_CLIP_W || otherCS.w < MIN_CLIP_W)
                    return float4(0.0, 0.0, 0.0, 1.0);

                float2 alongPixels = (otherCS.xy / otherCS.w - positionCS.xy / positionCS.w) * _ScreenParams.xy * lineEnd;
                float2 acrossPixels = normalize(float2(-alongPixels.y, alongPixels.x));
                positionCS.xy += acrossPixels * (side * 0.5 * _LineWidth) * 2.0 / _ScreenParams.xy * positionCS.w;
                return positionCS;
            }

            Varyings Vert(Attributes input) {
                Varyings output;
                float4 positionCS = ProjectAtInfinity(input.direction);
                if (_IsLine > 0.5)
                    positionCS = WidenLine(positionCS, input.otherEnd, input.shape.x, input.shape.w);
                positionCS.z = positionCS.w * FAR_PLANE_DEPTH;
                output.positionCS = positionCS;
                output.shape = input.shape;
                return output;
            }

            // A spike along one axis: bright and as wide as _SpikeWidth at the centre, thinner and fainter towards its
            // end. Narrower than a pixel it keeps a pixel's width and gets dimmer instead, so it does not flicker.
            float Spike(float along, float across, float pixel) {
                float taper = saturate(1.0 - abs(along) / _SpikeLength);
                float width = _SpikeWidth * taper;
                float drawnWidth = max(width, pixel);
                float ratio = across / drawnWidth;
                return _SpikeIntensity * taper * taper * (width / drawnWidth) * exp(-ratio * ratio);
            }

            // Scintillation: three sines at unrelated rates, so the flicker never settles into a visible rhythm. The sum
            // stays within -1..1; the phase keeps neighbouring stars apart.
            float Twinkle(float phase) {
                float t = _Time.y * _TwinkleSpeed + phase;
                float wave = 0.5 * sin(t) + 0.3 * sin(t * 2.37 + phase * 1.7) + 0.2 * sin(t * 5.13 + phase * 3.1);
                return 1.0 + _TwinkleAmount * wave;
            }

            // The whole star twinkles; the spikes and the halo swing a little more than the core, as through the air.
            half4 FragStar(float4 shape) {
                float2 position = shape.xy;
                float radius = length(position);
                float pixel = max(fwidth(position.x), fwidth(position.y));
                float twinkle = Twinkle(shape.w);
                float outerTwinkle = twinkle * twinkle;

                float coreRatio = radius / _CoreRadius;
                float core = _CoreIntensity * exp(-coreRatio * coreRatio) * twinkle;
                float haloFade = saturate(1.0 - radius / _HaloRadius);
                float halo = _HaloIntensity * haloFade * haloFade * haloFade * outerTwinkle;
                float spikes = (Spike(position.x, position.y, pixel) + Spike(position.y, position.x, pixel)) * outerTwinkle;
                return _Color * ((core + halo + spikes) * shape.z);
            }

            half4 FragLine(float4 shape) {
                float core = shape.x * LINE_CORE_SHARPNESS;
                float glow = shape.x * LINE_GLOW_SHARPNESS;
                float endFade = saturate(shape.y / _LineEndFade) * saturate((shape.z - shape.y) / _LineEndFade);
                float profile = _LineIntensity * exp(-core * core) + _LineGlowIntensity * exp(-glow * glow);
                return _Color * (profile * smoothstep(0.0, 1.0, endFade));
            }

            half4 Frag(Varyings input) : SV_Target {
                if (_IsLine > 0.5)
                    return FragLine(input.shape);
                return FragStar(input.shape);
            }
            ENDHLSL
        }
    }
}
