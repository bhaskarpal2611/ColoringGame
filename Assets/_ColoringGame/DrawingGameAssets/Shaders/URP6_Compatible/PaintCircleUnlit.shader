Shader "Custom/PaintCircleFull_URP6_AlphaColorMask"
{
    Properties
    {
        _MainTex ("Paint Texture", 2D) = "white" {}
        _Original ("Original Texture", 2D) = "white" {}
        _BrushColor ("Brush Color", Color) = (0,1,0,1)
        _UVPosition ("Circle Center (UV)", Vector) = (0.5, 0.5, 0, 0)
        _BrushSize ("Circle Radius", Float) = 0.3
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "PaintCircleFullPass"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_Original);
            SAMPLER(sampler_Original);

            float4 _BrushColor;
            float4 _UVPosition;
            float _BrushSize;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.uv = IN.uv;
                OUT.vertex = TransformObjectToHClip(IN.vertex.xyz);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                half4 paintBase = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);   // previous paint
                half4 underBase = SAMPLE_TEXTURE2D(_Original, sampler_Original, uv); // original sprite

                float dist = distance(uv, _UVPosition.xy);
                float edge = 0.015;
                float circleMask = smoothstep(_BrushSize, _BrushSize - edge, dist);

                // Only prevent painting over original black outlines, ignore already painted areas
                float3 origCol = underBase.rgb;
                float isNotBlack = step(0.1, origCol.r) * step(0.1, origCol.g) * step(0.1, origCol.b);

                float mask = circleMask * underBase.a * isNotBlack;

                half4 newPaint = lerp(paintBase, _BrushColor, mask * _BrushColor.a);
    
                return half4(newPaint.rgb, underBase.a);
            }

            ENDHLSL
        }
    }
}
