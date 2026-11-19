Shader "Project/Water Stream"
{
    Properties
    {
        _CoreColor ("Core Color", Color) = (0.62, 0.8, 0.92, 0.32)
        _EdgeColor ("Edge Color", Color) = (0.88, 0.96, 1, 0.92)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        _NoiseScale ("Noise Scale (Along, Around)", Vector) = (2.2, 1.6, 0, 0)
        _ScrollSpeed ("Scroll Speed", Float) = 7
        _FresnelPower ("Fresnel Power", Float) = 2.2
        _FoamThreshold ("Foam Threshold", Range(0, 1)) = 0.58
        _FoamAmount ("Foam Amount", Range(0, 1)) = 0.7
        _BreakupSoftness ("Breakup Softness", Range(0.01, 0.5)) = 0.12
        _SpecularPower ("Specular Power", Float) = 64
        _SpecularStrength ("Specular Strength", Float) = 1.4
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 color : COLOR;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _EdgeColor;
                half4 _FoamColor;
                float4 _NoiseScale;
                float _ScrollSpeed;
                float _FresnelPower;
                float _FoamThreshold;
                float _FoamAmount;
                float _BreakupSoftness;
                float _SpecularPower;
                float _SpecularStrength;
            CBUFFER_END

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float ValueNoise(float3 p)
            {
                float3 cell = floor(p);
                float3 local = frac(p);
                float3 blend = local * local * (3.0 - 2.0 * local);

                float bottom = lerp(
                    lerp(Hash(cell), Hash(cell + float3(1, 0, 0)), blend.x),
                    lerp(Hash(cell + float3(0, 1, 0)), Hash(cell + float3(1, 1, 0)), blend.x), blend.y);
                float top = lerp(
                    lerp(Hash(cell + float3(0, 0, 1)), Hash(cell + float3(1, 0, 1)), blend.x),
                    lerp(Hash(cell + float3(0, 1, 1)), Hash(cell + float3(1, 1, 1)), blend.x), blend.y);
                return lerp(bottom, top, blend.z);
            }

            float Fbm(float3 p)
            {
                float sum = 0.0;
                float amplitude = 0.5;

                for (int octave = 0; octave < 4; octave++)
                {
                    sum += ValueNoise(p) * amplitude;
                    p = p * 2.03 + 11.7;
                    amplitude *= 0.5;
                }

                return sum / 0.9375;
            }

            float3 StreamCoordinates(float2 uv, float alongScale, float aroundScale, float scroll)
            {
                float angle = uv.y * TWO_PI;
                return float3(uv.x * alongScale - scroll, cos(angle) * aroundScale, sin(angle) * aroundScale);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float breakup = input.color.r;
                float scroll = _Time.y * _ScrollSpeed;

                float streaks = Fbm(StreamCoordinates(input.uv, _NoiseScale.x, _NoiseScale.y, scroll));
                float blobs = Fbm(StreamCoordinates(input.uv, _NoiseScale.x * 2.5, _NoiseScale.y * 1.5, scroll * 1.1) + 5.3);

                float3 normal = normalize(input.normalWS);
                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                float fresnel = pow(1.0 - saturate(abs(dot(normal, view))), _FresnelPower);

                float3 color = lerp(_CoreColor.rgb, _EdgeColor.rgb, fresnel);
                float alpha = lerp(_CoreColor.a, _EdgeColor.a, fresnel);

                float foam = smoothstep(_FoamThreshold, _FoamThreshold + 0.2, streaks) * lerp(_FoamAmount, 1.0, breakup);
                color = lerp(color, _FoamColor.rgb, foam);
                alpha = max(alpha, foam * _FoamColor.a);

                Light mainLight = GetMainLight();
                float diffuse = saturate(dot(normal, mainLight.direction)) * 0.5 + 0.5;
                float3 ambient = SampleSH(normal);
                color *= ambient + mainLight.color * diffuse;

                float3 halfVector = normalize(mainLight.direction + view);
                float specular = pow(saturate(dot(normal, halfVector)), _SpecularPower) * _SpecularStrength;
                color += mainLight.color * specular;
                alpha = saturate(alpha + specular);

                float breakupMask = smoothstep(breakup - _BreakupSoftness, breakup + _BreakupSoftness, blobs * (1.0 - breakup * 0.35) + 0.15);
                alpha *= breakupMask * input.color.a;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
