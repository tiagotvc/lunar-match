// Based on Universal Render Pipeline/2D/Sprite-Unlit-Default (com.unity.render-pipelines.universal
// Shaders/2D/Sprite-Unlit-Default.shader) with a world-space vertex offset added.
//
// Bows every sprite horizontally based on how far it is, vertically, from the camera's
// current position - squared, so the curve grows toward the top/bottom of the screen and
// stays flat near the camera's vertical center. Assign this same shader/material to every
// map element (background, path, nodes, decorations) so they all bend together as one
// surface instead of independently - that's what reads as "wrapping around a globe"
// instead of a flat plane, using only an orthographic 2D camera (no perspective trick).
Shader "Custom/CurvedWorldSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        [MaterialToggle] _ZWrite("ZWrite", Float) = 0
        _Curvature ("Curvature", Float) = 0.01

        // Legacy properties, present so materials using this shader can fall back to the
        // default sprite shader gracefully (same as the stock URP sprite shader).
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        [HideInInspector] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex CurvedUnlitVertex
            #pragma fragment UnlitFragment

            struct Attributes
            {
                COMMON_2D_INPUTS
                half4 color : COLOR;
                UNITY_SKINNED_VERTEX_INPUTS
            };

            struct Varyings
            {
                COMMON_2D_OUTPUTS
                half4 color : COLOR;
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"

            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Curvature;
            CBUFFER_END

            Varyings CurvedUnlitVertex(Attributes input)
            {
                UNITY_SKINNED_VERTEX_COMPUTE(input);
                SetUpSpriteInstanceProperties();
                input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);

                UNITY_SETUP_INSTANCE_ID(input);
                Varyings o = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float dy = positionWS.y - _WorldSpaceCameraPos.y;
                positionWS.x += dy * dy * _Curvature;

                o.positionCS = TransformWorldToHClip(positionWS);
            #if defined(DEBUG_DISPLAY)
                o.positionWS = positionWS;
            #endif
                o.uv = input.uv;
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 UnlitFragment(Varyings input) : SV_Target
            {
                return CommonUnlitFragment(input, input.color);
            }
            ENDHLSL
        }
    }
}
