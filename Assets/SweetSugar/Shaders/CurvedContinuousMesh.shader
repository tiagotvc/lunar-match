// Plain mesh shader (not sprite-specific) for the continuous, subdivided map background
// mesh built by Assets/SweetSugar/Editor/BuildContinuousMapBackground.cs. Deliberately
// avoids the SpriteRenderer-specific machinery in CurvedWorldSprite.shader (UnityFlipSprite,
// unity_SpriteProps instancing color, COMMON_2D_INPUTS) since a generated Mesh rendered via
// a plain MeshRenderer doesn't have a SpriteRenderer feeding those per-instance properties -
// reusing that shader here would silently zero out the mesh (UnityFlipSprite multiplies
// position by an unbound flip value).
//
// Same curvature math as CurvedWorldSprite: compresses X toward the camera's X, growing
// with the squared vertical distance from the camera - a symmetric squeeze that reads as
// receding toward a horizon, not a one-directional lean. Because this is one continuous
// mesh with many vertices instead of five separate 4-vertex sprite quads, the curve is
// smooth across the whole background with no seams between what used to be five separate
// tiles.
Shader "Custom/CurvedContinuousMesh"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Curvature ("Curvature", Float) = 0.0005
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Curvature;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings o;

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float dy = positionWS.y - _WorldSpaceCameraPos.y;
                float squeeze = saturate(1.0 - _Curvature * dy * dy);
                positionWS.x = _WorldSpaceCameraPos.x + (positionWS.x - _WorldSpaceCameraPos.x) * squeeze;

                o.positionCS = TransformWorldToHClip(positionWS);
                o.uv = input.uv;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                // Tile the V coordinate manually (frac) instead of relying on the texture's
                // import wrap mode, since the source texture (Map_1.png) is imported as
                // Clamp for its original single-tile sprite use.
                float2 uv = float2(input.uv.x, frac(input.uv.y));
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
            }
            ENDHLSL
        }
    }
}
