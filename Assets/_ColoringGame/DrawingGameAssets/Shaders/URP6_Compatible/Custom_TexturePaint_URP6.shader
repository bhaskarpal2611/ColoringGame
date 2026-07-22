Shader "Custom/PaintCircleTexture_URP6"
{
    Properties
    {
        _MainTex ("Paint Texture", 2D) = "white" {}       // Accumulated canvas RT
        _Original ("Original Texture", 2D) = "white" {}   // Sprite base
        _BrushTexture ("Brush Pattern", 2D) = "white" {}  // The brush stamp texture
        _BrushSize ("Circle Radius", Float) = 0.3
        _UVPosition ("Brush Center (UV)", Vector) = (0.5, 0.5, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Name "PaintCircleTexturePass"
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
            TEXTURE2D(_BrushTexture);
            SAMPLER(sampler_BrushTexture);

            float _BrushSize;
            float4 _UVPosition;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.uv = IN.uv;
                OUT.vertex = TransformObjectToHClip(IN.vertex.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half2 uv = IN.uv;

                half4 paintBase = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 underBase = SAMPLE_TEXTURE2D(_Original, sampler_Original, uv);

                half dist = distance(uv, _UVPosition.xy);
                half circleMask = step(dist, _BrushSize);

                half4 brushSample = SAMPLE_TEXTURE2D(_BrushTexture, sampler_BrushTexture, uv);

                half isNotBlack = step(0.1h, underBase.r) * step(0.1h, underBase.g) * step(0.1h, underBase.b);
                half mask = circleMask * underBase.a * isNotBlack * brushSample.a;

                half4 stampedBrush = brushSample * mask;

                half4 outColor = lerp(paintBase, stampedBrush, mask);

                return half4(outColor.rgb, underBase.a);
            }

            ENDHLSL
        }
    }
}
