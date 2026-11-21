Shader "Project/Burnable"
{
    Properties
    {
        [MainTexture] _BaseMap ("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor ("Color", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.35
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5

        [Header(Char)]
        _CharColor ("Char Color", Color) = (0.035, 0.03, 0.027, 1)
        _AshColor ("Ash Color", Color) = (0.32, 0.31, 0.29, 1)
        _CharSmoothness ("Char Smoothness", Range(0, 1)) = 0.04
        _NoiseScale ("Char Noise Cells per Meter", Float) = 2.6
        _EdgeWidth ("Char Front Width", Range(0.02, 0.6)) = 0.1
        _SootStrength ("Soot Darkening Ahead of the Front", Range(0, 1)) = 0.8
        _AshAmount ("Ash Share of Charred Surface", Range(0, 1)) = 0.22

        [Header(Embers)]
        [HDR] _EmberColor ("Ember Color", Color) = (2.2, 0.5, 0.08, 1)
        _CrackScale ("Crack Cells per Meter", Float) = 7
        _CrackSharpness ("Crack Sharpness", Float) = 24
        _CrackGlow ("Crack Glow", Range(0, 1)) = 0.45

        [Header(Per Renderer)]
        _Char ("Char", Range(0, 1)) = 0
        _Heat ("Heat", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" "UniversalMaterialType"="Lit" "IgnoreProjector"="True" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Smoothness;
            half _Cutoff;
            half4 _CharColor;
            half4 _AshColor;
            half _CharSmoothness;
            float _NoiseScale;
            float _EdgeWidth;
            half _SootStrength;
            half _AshAmount;
            half4 _EmberColor;
            float _CrackScale;
            float _CrackSharpness;
            half _CrackGlow;
            half _Char;
            half _Heat;
        CBUFFER_END

        TEXTURE3D(_CharVolume);
        SAMPLER(sampler_CharVolume);
        float4x4 _CharVolumeMatrix;
        float _CharVolumeEnabled;

        #define MAX_MOVING_CHAR_ZONES 16

        float4 _MovingCharZones[MAX_MOVING_CHAR_ZONES];
        float4 _MovingCharZoneStates[MAX_MOVING_CHAR_ZONES];
        float _MovingCharZoneCount;

        float BurnHash(float3 position)
        {
            position = frac(position * 0.3183099 + 0.1);
            position *= 17.0;
            return frac(position.x * position.y * position.z * (position.x + position.y + position.z));
        }

        float BurnValueNoise(float3 position)
        {
            float3 cell = floor(position);
            float3 blend = frac(position);
            blend = blend * blend * (3.0 - 2.0 * blend);

            float bottom = lerp(
                lerp(BurnHash(cell), BurnHash(cell + float3(1, 0, 0)), blend.x),
                lerp(BurnHash(cell + float3(0, 1, 0)), BurnHash(cell + float3(1, 1, 0)), blend.x),
                blend.y);
            float top = lerp(
                lerp(BurnHash(cell + float3(0, 0, 1)), BurnHash(cell + float3(1, 0, 1)), blend.x),
                lerp(BurnHash(cell + float3(0, 1, 1)), BurnHash(cell + float3(1, 1, 1)), blend.x),
                blend.y);

            return lerp(bottom, top, blend.z);
        }

        float BurnFractalNoise(float3 position)
        {
            return BurnValueNoise(position) * 0.55
                + BurnValueNoise(position * 2.03 + 17.1) * 0.3
                + BurnValueNoise(position * 4.11 + 31.7) * 0.15;
        }

        void SampleCharVolume(float3 positionWS, out float charAmount, out float heat)
        {
            float3 uvw = mul(_CharVolumeMatrix, float4(positionWS, 1.0)).xyz;
            float2 volume = SAMPLE_TEXTURE3D_LOD(_CharVolume, sampler_CharVolume, uvw, 0).rg * _CharVolumeEnabled;
            float inside = all(uvw == saturate(uvw)) ? 1.0 : 0.0;
            charAmount = volume.r * inside;
            heat = volume.g * inside;
        }

        void SampleMovingCharZones(float3 positionWS, inout float charAmount, inout float heat)
        {
            int count = min((int)_MovingCharZoneCount, MAX_MOVING_CHAR_ZONES);

            for (int index = 0; index < count; index++)
            {
                float4 zone = _MovingCharZones[index];
                float4 state = _MovingCharZoneStates[index];
                float height = clamp(positionWS.y - zone.y, 0.0, state.x);
                float gap = max(0.0, distance(positionWS, float3(zone.x, zone.y + height, zone.z)) - zone.w);
                float weight = 1.0 - saturate(gap / max(state.w, 0.001));
                charAmount = max(charAmount, state.y * weight);
                heat = max(heat, state.z * weight * weight);
            }
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ LIGHTMAP_ON
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 noisePosition : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 5);
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positions.positionCS;
                output.positionWS = positions.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.noisePosition = mul((float3x3)GetObjectToWorldMatrix(), input.positionOS.xyz);
                output.fogFactor = ComputeFogFactor(positions.positionCS.z);
                OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
                OUTPUT_SH(output.normalWS, output.vertexSH);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float zoneChar;
                float zoneHeat;
                SampleCharVolume(input.positionWS, zoneChar, zoneHeat);
                SampleMovingCharZones(input.positionWS, zoneChar, zoneHeat);
                float burn = max(_Char, zoneChar);
                float heat = max(_Heat, zoneHeat);

                float3 noisePosition = input.noisePosition * _NoiseScale;
                float noise = saturate((BurnFractalNoise(noisePosition) - 0.22) / 0.56);
                float charMask = saturate((burn * (1.0 + _EdgeWidth) - noise) / _EdgeWidth);
                float front = charMask * (1.0 - charMask) * 4.0;
                front *= front * front;
                float sootWidth = _EdgeWidth * 4.0;
                float sootMask = saturate((burn * (1.0 + sootWidth) - noise) / sootWidth);
                float soot = sootMask * _SootStrength * (1.0 - charMask);
                float detail = BurnFractalNoise(noisePosition * 3.7 + 5.3);
                float ash = step(1.0 - _AshAmount, detail);
                float crackNoise = BurnFractalNoise(input.noisePosition * _CrackScale);
                float cracks = saturate(1.0 - abs(crackNoise - 0.5) * _CrackSharpness);
                cracks *= step(0.5, BurnValueNoise(noisePosition * 0.7 + 11.3));
                float glow = heat * heat;

                half4 baseSample = SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)) * _BaseColor;
                half3 charColor = lerp(_CharColor.rgb, _CharColor.rgb * 2.5, detail);
                charColor = lerp(charColor, _AshColor.rgb, ash);
                half3 albedo = lerp(baseSample.rgb * (1.0 - soot), charColor, charMask);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                surface.metallic = 0.0;
                surface.specular = half3(0.0, 0.0, 0.0);
                surface.smoothness = lerp(_Smoothness, _CharSmoothness, charMask);
                surface.occlusion = 1.0;
                surface.emission = _EmberColor.rgb * glow * (front + cracks * charMask * (1.0 - ash) * _CrackGlow);
                surface.alpha = 1.0;
                surface.normalTS = half3(0.0, 0.0, 1.0);

                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.positionCS = input.positionCS;
                lighting.normalWS = NormalizeNormalPerPixel(input.normalWS);
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.fogCoord = input.fogFactor;
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, lighting.normalWS);
                lighting.shadowMask = SAMPLE_SHADOWMASK(input.lightmapUV);

                half4 color = UniversalFragmentPBR(lighting, surface);
                color.rgb = MixFog(color.rgb, lighting.fogCoord);
                color.a = 1.0;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
