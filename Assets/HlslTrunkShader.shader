Shader "HlslTrunkShader"
{
    Properties
    {
        BaseColor_1("BaseColor1", Color) = (0.5754717, 0.3830971, 0.13301, 0)
        BaseColor_2("BaseColor2", Color) = (0.3867925, 0.2579577, 0.09304912, 0)
        PatternNoise_Float("PatternNoise", Float) = 3.3
        WindDensity_Float("Wind Density", Float) = 0.09
        WindStrength_Float("Wind Strength", Float) = 0.24
        WindSymmetry_Float("Wind Symetry", Range(-1, 1)) = 0
        WindDirection_Vector3("Wind Direction", Vector) = (1, 0, 0, 0)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue"="AlphaTest"
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS

            // Defines
            #define _NORMALMAP 1
            #define _SPECULAR_SETUP
            
            // Includes
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 tangentWS : TEXCOORD2;
                float4 texCoord0 : TEXCOORD3;
                float3 viewDirectionWS : TEXCOORD4;
                
                float4 fogFactorAndVertexLight : TEXCOORD7;
                float4 shadowCoord : TEXCOORD8;
            
            };

            float4 BaseColor_1;
            float4 BaseColor_2;
            float PatternNoise_Float;
            float WindDensity_Float;
            float WindStrength_Float;
            float WindSymmetry_Float;
            float3 WindDirection_Vector3;

            float2 Unity_GradientNoise_Dir_float(float2 p)
            {
                // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
                p = p % 289;
                // need full precision, otherwise half overflows when p > 1
                float x = float(34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }

            void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
            { 
                float2 p = UV * Scale;
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
                float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
            }

            
            Varyings vert(Attributes input)
            {
                Varyings varyings = (Varyings)0;

                float3 Normalized_WindDirection_Vector3 = normalize(WindDirection_Vector3);
                float2 Speeded_Time = _TimeParameters.x.xx *  float2 (6, 0);
                float2 Random_UV_Coordinates_For_Gradient_Noise = TransformObjectToWorld(input.positionOS).xy * float2 (1, 1) + Speeded_Time;
            
                // El noise va de 0 a 1. La idea es que el viento simulado no sea unidireccional. Por eso  originalmente se le restaba .5
                // a el valor para que los movimientos oscilen de [-0.5, 0.5]. 
                float GradientNoise_Value;
                Unity_GradientNoise_float(Random_UV_Coordinates_For_Gradient_Noise, WindDensity_Float, GradientNoise_Value);

                // A la WindSimmetry le hacemos lo inverso (el input es de -1 a 1, para que sea mas entendible para el usuario....osea nadie)
                float Abs_WindSymmetry_Float = (WindSymmetry_Float / 2) + 0.5;

                // Al ruido ahora se le resta los valores de la simetria del viento y se establece el rango de valores que van a sumar o restar a los vertices
                float Modified_Noise_Range = GradientNoise_Value - Abs_WindSymmetry_Float;
                
                float Wind_Strength_For_Vertex = Modified_Noise_Range * WindStrength_Float;
                // Distancia del vertice hasta el anclar que le asignamos, es importante "nullearla", para que aquellos vertices que correspondan a un
                // tronco, no se muevan
                float Distance_To_Anchor = distance(input.positionOS, input.uv1.xyz);
                float Anchor_Is_Not_Null = any(input.uv1.xyz);
                float Distance_To_Anchor_Or_Null = Distance_To_Anchor * Anchor_Is_Not_Null;
                float3 Directed_Wind_Vector = Wind_Strength_For_Vertex.xxx * Normalized_WindDirection_Vector3.xyz  * Distance_To_Anchor_Or_Null.xxx;
                                
                // Assign modified vertex attributes
                input.positionOS = Directed_Wind_Vector + input.positionOS;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                // Returns the camera relative position (if enabled)
                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);
                
                varyings.normalWS = normalWS;	
                varyings.positionCS = TransformWorldToHClip(positionWS);
                varyings.texCoord0 = input.uv0;
                varyings.viewDirectionWS = GetWorldSpaceViewDir(positionWS);
                varyings.shadowCoord = GetShadowCoord(vertexInput);

                return varyings;
            }

            half4 frag(Varyings varyings) : SV_TARGET
            {
                // must use interpolated tangent, bitangent and normal before they are normalized in the pixel shader.
                float3 unnormalizedNormalWS = varyings.normalWS;
                const float renormFactor = 1.0 / length(unnormalizedNormalWS);

                // Logica del frag shader especifica al arbol
                float4 UV0_Frac = frac(varyings.texCoord0);
                float4 Color_Lerp_Result = lerp(BaseColor_1, BaseColor_2, UV0_Frac);
                float _Property_02cc988a36c74c8896abb9c4618613f3_Out_0 = PatternNoise_Float;
                float Gradient_Noise;
                Unity_GradientNoise_float((TransformWorldToObject(varyings.positionWS).xy), PatternNoise_Float, Gradient_Noise);
                float Scaled_Gradient_Noise = Gradient_Noise * 0.04;
                float4 Color_With_Noise_Pattern = Color_Lerp_Result - (Scaled_Gradient_Noise.xxxx);
                
                float metallic            = 1;
                float specular            = IsGammaSpace() ? float3(0, 0, 0) : SRGBToLinear(float3(0, 0, 0));
                float smoothness          = 0.2;
                float occlusion           = 1.05;
                float emission            = float3(0, 0, 0);
                float alpha               = 1;
                float clearCoatMask       = 0;
                float clearCoatSmoothness = 1;

                
                // Este codigo esta mega copiado de esta libreria "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                // Este codigo general corresponde a Physically Based Materials, la abstraccion mas general de shaders para el render pipeline de unity
                // Por integridad academica lo lei, y borre llamadas inecesarias a funcionalidades que no usamos aca, asi entiendo mas de como funciona
                float bakedGI = SAMPLE_GI(float2(0,0), 0, varyings.normalWS);
                BRDFData brdfData;
                brdfData.diffuse = Color_With_Noise_Pattern.xyz;
                brdfData.specular = specular;
                brdfData.perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(smoothness);
                brdfData.roughness           = max(PerceptualRoughnessToRoughness(brdfData.perceptualRoughness), HALF_MIN_SQRT);
                brdfData.roughness2          = max(brdfData.roughness * brdfData.roughness, HALF_MIN);
                brdfData.grazingTerm         = saturate(smoothness);
    
                // Componente ambiental
                half3 indirectDiffuse = bakedGI * occlusion;
                half3 color = indirectDiffuse * brdfData.diffuse;
                
                // Suavidad
                half3 reflectVector = reflect(-varyings.viewDirectionWS, varyings.normalWS);
                half NoV = saturate(dot(varyings.normalWS, varyings.viewDirectionWS));
                half fresnelTerm = Pow4(1.0 - NoV);
                half mip = PerceptualRoughnessToMipmapLevel(brdfData.perceptualRoughness);
                half4 encodedIrradiance = SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVector, 0);
                half3 indirectSpecular = encodedIrradiance.rgb * occlusion;
                
                float surfaceReduction = 1.0 / (brdfData.roughness2 + 1.0);
                float specularSurfaceReduction = surfaceReduction * lerp(brdfData.specular, brdfData.grazingTerm, fresnelTerm);
                color += indirectSpecular * specularSurfaceReduction;

                // Componente difusa
                // To ensure backward compatibility we have to avoid using shadowMask input, as it is not present in older shaders
                half4 shadowMask = unity_ProbesOcclusion;
                float shadowAttenuation = MainLightShadow(varyings.shadowCoord, varyings.positionWS, shadowMask, _MainLightOcclusionProbes); 
                half NdotL = saturate(dot(varyings.normalWS, _MainLightPosition.xyz));
                half3 radiance = _MainLightColor.rgb * (shadowAttenuation * NdotL);
                color += brdfData.diffuse * radiance;
            
                return half4(color, 1);
            }


            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            
           
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"

            // --------------------------------------------------
            // Structs and Packing

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float4 uv1 : TEXCOORD1;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };
      

            float4 BaseColor_1;
            float4 BaseColor_2;
            float PatternNoise_Float;
            float WindDensity_Float;
            float WindStrength_Float;
            float WindSymmetry_Float;
            float3 WindDirection_Vector3;
            

            float2 Unity_GradientNoise_Dir_float(float2 p)
            {
                // Permutation and hashing used in webgl-nosie goo.gl/pX7HtC
                p = p % 289;
                // need full precision, otherwise half overflows when p > 1
                float x = float(34 * p.x + 1) * p.x % 289 + p.y;
                x = (34 * x + 1) * x % 289;
                x = frac(x / 41) * 2 - 1;
                return normalize(float2(x - floor(x + 0.5), abs(x) - 0.5));
            }

            void Unity_GradientNoise_float(float2 UV, float Scale, out float Out)
            { 
                float2 p = UV * Scale;
                float2 ip = floor(p);
                float2 fp = frac(p);
                float d00 = dot(Unity_GradientNoise_Dir_float(ip), fp);
                float d01 = dot(Unity_GradientNoise_Dir_float(ip + float2(0, 1)), fp - float2(0, 1));
                float d10 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 0)), fp - float2(1, 0));
                float d11 = dot(Unity_GradientNoise_Dir_float(ip + float2(1, 1)), fp - float2(1, 1));
                fp = fp * fp * fp * (fp * (fp * 6 - 15) + 10);
                Out = lerp(lerp(d00, d01, fp.y), lerp(d10, d11, fp.y), fp.x) + 0.5;
            }

            
            float3 _LightDirection;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                float3 Normalized_WindDirection_Vector3 = normalize(WindDirection_Vector3);
                float2 Speeded_Time = _TimeParameters.x.xx *  float2 (6, 0);
                float2 Random_UV_Coordinates_For_Gradient_Noise = TransformObjectToWorld(input.positionOS).xy * float2 (1, 1) + Speeded_Time;
            
                // El noise va de 0 a 1. La idea es que el viento simulado no sea unidireccional. Por eso  originalmente se le restaba .5
                // a el valor para que los movimientos oscilen de [-0.5, 0.5]. 
                float GradientNoise_Value;
                Unity_GradientNoise_float(Random_UV_Coordinates_For_Gradient_Noise, WindDensity_Float, GradientNoise_Value);

                // A la WindSimmetry le hacemos lo inverso (el input es de -1 a 1, para que sea mas entendible para el usuario....osea nadie)
                float Abs_WindSymmetry_Float = (WindSymmetry_Float / 2) + 0.5;

                // Al ruido ahora se le resta los valores de la simetria del viento y se establece el rango de valores que van a sumar o restar a los vertices
                float Modified_Noise_Range = GradientNoise_Value - Abs_WindSymmetry_Float;
                
                float Wind_Strength_For_Vertex = Modified_Noise_Range * WindStrength_Float;
                // Distancia del vertice hasta el anclar que le asignamos, es importante "nullearla", para que aquellos vertices que correspondan a un
                // tronco, no se muevan
                float Distance_To_Anchor = distance(input.positionOS, input.uv1.xyz);
                float Anchor_Is_Not_Null = any(input.uv1.xyz);
                float Distance_To_Anchor_Or_Null = Distance_To_Anchor * Anchor_Is_Not_Null;
                float3 Directed_Wind_Vector = Wind_Strength_For_Vertex.xxx * Normalized_WindDirection_Vector3.xyz  * Distance_To_Anchor_Or_Null.xxx;
                                
                // Assign modified vertex attributes
                input.positionOS = Directed_Wind_Vector + input.positionOS;
                
                // Returns the camera relative position (if enabled)
                float3 positionWS = TransformObjectToWorld( input.positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                return output;
            }

            half4 frag(Varyings output) : SV_TARGET 
            {    
                return 0;
            }

            ENDHLSL
        }
    }
    CustomEditor "ShaderGraph.PBRMasterGUI"
    FallBack "Hidden/Shader Graph/FallbackError"
}