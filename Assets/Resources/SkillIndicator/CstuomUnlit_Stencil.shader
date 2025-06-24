Shader "Custom/Indicator_Stencil_Unlit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,0,1,0.5)
        _MainTex ("Texture", 2D) = "white" {}
        _HeightOffset ("Height Offset", Float) = 0.1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _HeightOffset;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                // Y 위치에 오프셋 추가
                float3 positionWithOffset = IN.positionOS.xyz + float3(0, _HeightOffset, 0);
                OUT.positionHCS = TransformObjectToHClip(positionWithOffset);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _BaseColor;
                return col;
            }
            ENDHLSL
        }
    }
}