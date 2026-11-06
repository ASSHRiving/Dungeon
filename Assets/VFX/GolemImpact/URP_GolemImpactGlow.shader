Shader "Custom/URP_GolemImpactGlow"
{
    Properties
    {
        [MainTexture] _BaseMap("Voronoi Fracture Map (R:Crack G:TileID B:Bevel A:Mask)", 2D) = "white" {}
        [HideInInspector] _MainTex("Fallback MainTex", 2D) = "white" {}
        [HDR] _GlowColor("Crack Magma / Impact Glow", Color) = (3.8, 0.85, 0.15, 1.0)
        _SlateDarkColor("Dungeon Slate Dark (#23272A)", Color) = (0.14, 0.15, 0.17, 1.0)
        _SlateLightColor("Dungeon Slate Light (#464E54)", Color) = (0.27, 0.31, 0.33, 1.0)
        _BevelColor("Tile Bevel Highlight (#636E75)", Color) = (0.39, 0.43, 0.46, 1.0)
        _GlowFade("Glow Cooling Speed", Range(0.5, 3.0)) = 1.6
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "StylizedVoronoiImpact"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float4 uv0        : TEXCOORD0; // xy: UV, z: Custom1.x (0..1 Dissolve)
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                float  progress   : TEXCOORD1;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _GlowColor;
                half4  _SlateDarkColor;
                half4  _SlateLightColor;
                half4  _BevelColor;
                half   _GlowFade;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                o.uv = TRANSFORM_TEX(input.uv0.xy, _BaseMap);
                o.color = input.color;
                o.progress = saturate(max(input.uv0.z, 1.0 - input.color.a));
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                if (tex.a <= 0.01) discard;

                half crackMask = tex.r; // Voronoi 直線裂縫
                half tileId    = tex.g; // 每塊多邊形石板的獨立色階/ID
                half bevelMask = tex.b; // 石板內緣倒角線

                // 1. 逐塊石板崩塌溶解：利用每塊石板獨立的 tileId 與進度比對，讓外圍石板一塊塊消失
                half tileThreshold = lerp(-0.15, 1.05, input.progress);
                half tileAlive = step(tileThreshold, tileId + crackMask * 0.25);

                // 2. 組合地牢石板本體顏色 (深灰藍底色 + 塊狀色差 + 邊緣亮灰倒角)
                half3 slateRGB = lerp(_SlateDarkColor.rgb, _SlateLightColor.rgb, tileId);
                slateRGB = lerp(slateRGB, _BevelColor.rgb, bevelMask * 0.65);

                // 3. 裂縫火光隨時間冷卻轉為深黑裂隙
                half glowHeat = saturate(1.0 - input.progress * _GlowFade);
                half3 crackRGB = lerp(_SlateDarkColor.rgb * 0.35, _GlowColor.rgb, glowHeat);

                // 4. 混合石板與裂縫
                half3 finalRGB = lerp(slateRGB, crackRGB, smoothstep(0.15, 0.55, crackMask));
                half finalAlpha = saturate(tex.a * tileAlive * input.color.a);

                if (finalAlpha <= 0.01) discard;
                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}