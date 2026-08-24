Shader "Custom/RenderFeature/KawaseBlur"
{
    Properties
    {
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        ZWrite Off
        Cull Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _offset;

            half4 frag(Varyings input) : SV_Target
            {
                float2 res = _BlitSource_TexelSize.xy;
                float i = _offset;

                half4 col;
                col.rgb = SAMPLE_TEXTURE2D(_BlitSource, sampler_LinearClamp, input.texcoord).rgb;
                col.rgb += SAMPLE_TEXTURE2D(_BlitSource, sampler_LinearClamp, input.texcoord + float2( i,  i) * res).rgb;
                col.rgb += SAMPLE_TEXTURE2D(_BlitSource, sampler_LinearClamp, input.texcoord + float2( i, -i) * res).rgb;
                col.rgb += SAMPLE_TEXTURE2D(_BlitSource, sampler_LinearClamp, input.texcoord + float2(-i,  i) * res).rgb;
                col.rgb += SAMPLE_TEXTURE2D(_BlitSource, sampler_LinearClamp, input.texcoord + float2(-i, -i) * res).rgb;
                col.rgb /= 5.0h;

                return col;
            }
            ENDHLSL
        }
    }
}
