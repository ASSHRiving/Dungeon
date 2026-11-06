Shader "Custom/URP_VoronoiCrackDecal"
{
    Properties
    {
        _MainTex ("Stone Crack Texture (RGBA)", 2D) = "white" {}
        _RimBoost ("Crack Rim Brightness", Range(1.0, 4.0)) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "StoneCrackForward"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float4 uvAndCustom : TEXCOORD0; // xy: uv, z: Custom1.x (fade 0->1)
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float erosion : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half _RimBoost;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uvAndCustom.xy, _MainTex);
                OUT.color = IN.color;
                OUT.erosion = saturate(IN.uvAndCustom.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // 從外圍裂紋尖端往中心逐漸收合消散
                float2 centeredUV = IN.uv * 2.0 - 1.0;
                half radialDist = length(centeredUV);
                half radialFade = 1.0 - smoothstep(1.0 - IN.erosion, 1.15 - IN.erosion, radialDist * 0.85);

                half finalAlpha = saturate(tex.a * 1.5) * radialFade * IN.color.a;
                if (finalAlpha <= 0.01)
                    discard;

                // 提亮裂紋邊緣的銀灰石頭崩裂面，保留深色裂縫中心
                half3 finalRGB = tex.rgb * _RimBoost * IN.color.rgb;
                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}