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

                half4 paintBase = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 underBase = SAMPLE_TEXTURE2D(_Original, sampler_Original, uv);

                float dist = distance(uv, _UVPosition.xy);
                float circleMask = step(dist, _BrushSize);

                // Color threshold to detect "near black" outlines
                float colorThreshold = 0.05; // Adjust as needed
                float isNotBlack = step(colorThreshold, length(underBase.rgb));

                // Updated: restrict mask by alpha AND color threshold
                float mask = circleMask * underBase.a * isNotBlack;

                half4 newPaint = lerp(paintBase, _BrushColor, mask * _BrushColor.a);
                half4 finalRGB = lerp(underBase, newPaint, mask * _BrushColor.a);

                float alpha = underBase.a;
                return half4(finalRGB.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
