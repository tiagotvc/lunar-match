// Full-screen post-process that squeezes the rendered frame horizontally toward its
// center as you move away (vertically) from a focus row, growing with the square of
// that distance. That reads as "the world curving away into a horizon" (top and bottom
// of the screen narrowing toward a vanishing point) instead of the whole scene sliding
// sideways - the effect the per-sprite CurvedWorldSprite shader could not produce, since
// that displaced individual sprites rather than warping the final composited image.
//
// Meant to be used as the Pass Material on a "Full Screen Pass Renderer Feature" added to
// the URP 2D Renderer asset (Assets/Settings/Renderer2D.asset) - built into URP 17, no
// custom C# needed. Written against the real Blit.hlsl in the installed package
// (com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl) for the Vert function and
// _BlitTexture binding, not guessed.
Shader "Custom/GlobeHorizonFullscreen"
{
    Properties
    {
        _Curvature ("Curvature", Range(0, 2)) = 0.3
        _VerticalCenter ("Vertical Center (0 = bottom of screen, 1 = top)", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "GlobeHorizon"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Curvature;
                float _VerticalCenter;
            CBUFFER_END

            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;
                float cy = uv.y - _VerticalCenter;

                // squeeze == 1 at the focus row (no distortion), shrinking toward 0 as we
                // move away from it - *4 so the very top/bottom edge (cy = +-0.5) reaches
                // full Curvature strength.
                float squeeze = 1.0 - _Curvature * (cy * cy) * 4.0;
                squeeze = clamp(squeeze, 0.05, 1.0);

                float2 sampledUV = uv;
                sampledUV.x = 0.5 + (uv.x - 0.5) * squeeze;

                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_LinearClamp, sampledUV, 0);
            }
            ENDHLSL
        }
    }
}
