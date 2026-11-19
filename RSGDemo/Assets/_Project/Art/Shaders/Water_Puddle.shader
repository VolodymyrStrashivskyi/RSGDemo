Shader "Project/Water Puddle"
{
    Properties
    {
        _WaterColor ("Water Color", Color) = (0.16, 0.21, 0.26, 0.6)
        _DampColor ("Damp Rim Color", Color) = (0.03, 0.035, 0.04, 0.4)
        _FoamColor ("Foam Color", Color) = (0.92, 0.97, 1, 0.85)
        _EdgeNoiseScale ("Edge Noise Scale", Float) = 1.7
        _EdgeWobble ("Edge Wobble", Range(0, 0.5)) = 0.3
        _DampWidth ("Damp Rim Width", Range(0, 0.4)) = 0.25
        _RippleFrequency ("Ripple Frequency", Float) = 22
        _RippleSpeed ("Ripple Speed", Float) = 9
        _RippleDecay ("Ripple Decay", Float) = 1.6
        _RippleStrength ("Ripple Strength", Float) = 0.35
        _WindStrength ("Calm Surface Wobble", Float) = 0.04
        _FoamRadius ("Foam Radius", Float) = 0.35
        _CrestColor ("Ripple Crest Color", Color) = (0.85, 0.93, 1, 0.35)
        _BaseReflection ("Base Reflection", Range(0, 1)) = 0.18
        _DropRippleScale ("Drop Ripple Cells per Meter", Float) = 3.5
        _DropRippleRate ("Drop Ripples per Second", Float) = 1.6
        _DropRippleStrength ("Drop Ripple Strength", Float) = 0.9
        _DropRippleFalloff ("Drop Ripple Falloff, 1/m", Float) = 1.1
        _Roughness ("Roughness", Range(0, 1)) = 0.06
        _SpecularPower ("Specular Power", Float) = 180
        _SpecularStrength ("Specular Strength", Float) = 2.5
        _WetnessTex ("Wetness (water depth, m)", 2D) = "black" {}
        _FullDepth ("Depth Looking Fully Wet, m", Float) = 0.004
        _WaterThreshold ("Standing Water Threshold", Range(0, 1)) = 0.4
        _ImpactPosition ("Impact Position", Vector) = (0, 0, 0, 0)
        _Activity ("Activity", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent-10" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _WaterColor;
                half4 _DampColor;
                half4 _FoamColor;
                float _EdgeNoiseScale;
                float _EdgeWobble;
                float _DampWidth;
                float _RippleFrequency;
                float _RippleSpeed;
                float _RippleDecay;
                float _RippleStrength;
                float _WindStrength;
                float _FoamRadius;
                half4 _CrestColor;
                float _BaseReflection;
                float _DropRippleScale;
                float _DropRippleRate;
                float _DropRippleStrength;
                float _DropRippleFalloff;
                float _Roughness;
                float _SpecularPower;
                float _SpecularStrength;
                float _FullDepth;
                float _WaterThreshold;
                float4 _ImpactPosition;
                float _Activity;
            CBUFFER_END

            TEXTURE2D(_WetnessTex);
            SAMPLER(sampler_WetnessTex);

            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                float2 blend = local * local * (3.0 - 2.0 * local);
                float bottom = lerp(Hash(cell), Hash(cell + float2(1, 0)), blend.x);
                float top = lerp(Hash(cell + float2(0, 1)), Hash(cell + float2(1, 1)), blend.x);
                return lerp(bottom, top, blend.y);
            }

            float Fbm(float2 p)
            {
                float sum = 0.0;
                float amplitude = 0.5;

                for (int octave = 0; octave < 4; octave++)
                {
                    sum += ValueNoise(p) * amplitude;
                    p = p * 2.07 + 17.1;
                    amplitude *= 0.5;
                }

                return sum / 0.9375;
            }

            float2 NoiseGradient(float2 p)
            {
                const float delta = 0.05;
                float center = Fbm(p);
                return float2(Fbm(p + float2(delta, 0)) - center, Fbm(p + float2(0, delta)) - center) / delta;
            }

            float2 Hash2(float2 p)
            {
                return float2(Hash(p), Hash(p + 19.19));
            }

            float3 DropRipples(float2 positionWS, float time)
            {
                float2 scaled = positionWS * _DropRippleScale;
                float2 cell = floor(scaled);
                float2 slope = 0;
                float crest = 0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbour = cell + float2(x, y);
                        float2 random = Hash2(neighbour);
                        float age = frac(time * _DropRippleRate + random.x);
                        float2 toPoint = scaled - (neighbour + random);
                        float distanceToDrop = length(toPoint);
                        float ringRadius = age * 1.2;
                        float ring = distanceToDrop - ringRadius;
                        float envelope = (1.0 - age) * (1.0 - age) * smoothstep(0.25, 0.0, abs(ring));
                        float wave = sin(ring * 30.0) * envelope;
                        slope += toPoint / max(distanceToDrop, 1e-4) * wave;
                        crest += saturate(cos(ring * 30.0)) * envelope;
                    }
                }

                return float3(slope, crest);
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 offset = input.positionWS.xz - _ImpactPosition.xz;
                float distanceWS = length(offset);
                float2 radial = offset / max(distanceWS, 1e-4);

                float wetness = SAMPLE_TEXTURE2D(_WetnessTex, sampler_WetnessTex, input.uv).r / _FullDepth;
                float edgeNoise = (Fbm(input.positionWS.xz * _EdgeNoiseScale) - 0.5) * _EdgeWobble * 2.0;
                float shaped = wetness + edgeNoise * saturate(wetness * 4.0);
                float waterMask = smoothstep(_WaterThreshold - 0.08, _WaterThreshold + 0.08, shaped);
                float dampMask = smoothstep(0.0, _DampWidth, shaped) * (1.0 - waterMask);
                float depth = saturate((shaped - _WaterThreshold) / (1.0 - _WaterThreshold));

                if (waterMask + dampMask <= 0.001)
                    discard;

                float time = _Time.y;
                float phase = distanceWS * _RippleFrequency - time * _RippleSpeed;
                float rippleFalloff = exp(-distanceWS * _RippleDecay) * _Activity * waterMask;
                float slope = cos(phase) * rippleFalloff * _RippleStrength;

                float2 jitter = NoiseGradient(input.positionWS.xz * 3.0 + time * float2(0.35, 0.2));
                float2 splashJitter = NoiseGradient(input.positionWS.xz * 9.0 - time * 2.5) * _Activity * smoothstep(_FoamRadius * 2.0, 0.0, distanceWS);
                float3 drops = DropRipples(input.positionWS.xz, time) * (_Activity * waterMask * exp(-distanceWS * _DropRippleFalloff));
                float2 tilt = radial * slope + jitter * _WindStrength + splashJitter * 0.06 + drops.xy * _DropRippleStrength * 0.1;
                float3 normal = normalize(float3(-tilt.x, 1.0, -tilt.y));

                float3 view = normalize(GetWorldSpaceViewDir(input.positionWS));
                float viewDot = saturate(dot(normal, view));
                float fresnel = _BaseReflection + (1.0 - _BaseReflection) * pow(1.0 - viewDot, 5.0);

                float3 reflection = GlossyEnvironmentReflection(reflect(-view, normal), _Roughness, 1.0);
                Light mainLight = GetMainLight();
                float3 halfVector = normalize(mainLight.direction + view);
                float specular = pow(saturate(dot(normal, halfVector)), _SpecularPower) * _SpecularStrength;

                float3 color = lerp(_WaterColor.rgb, reflection, fresnel) + mainLight.color * specular;
                float alpha = lerp(_WaterColor.a, 1.0, fresnel) * lerp(0.75, 1.0, depth);

                float crest = saturate(sin(phase)) * rippleFalloff * 0.9 + drops.z * 0.6;
                color = lerp(color, _CrestColor.rgb, saturate(crest) * _CrestColor.a);
                alpha = max(alpha, saturate(crest) * _CrestColor.a);

                float foamNoise = Fbm(input.positionWS.xz * 7.0 + radial * time * 1.5);
                float foam = smoothstep(0.45, 0.7, foamNoise) * smoothstep(_FoamRadius, _FoamRadius * 0.2, distanceWS) * _Activity;
                color = lerp(color, _FoamColor.rgb, foam * _FoamColor.a);
                alpha = max(alpha, foam * _FoamColor.a);

                float waterAlpha = saturate(alpha + specular) * waterMask;
                float3 finalColor = lerp(_DampColor.rgb, color, waterMask);
                float finalAlpha = waterAlpha + _DampColor.a * dampMask;
                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}
