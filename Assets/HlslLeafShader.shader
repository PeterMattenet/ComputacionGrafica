Shader "HlslLeafShader"
{
    Properties
    {
        WindMovement_Vec2("Wind Movement", Vector) = (6, 0, 0, 0)
        WindDensity_Float("Wind Density", Float) = 0.09
        WindStrength_Float("Wind Strength", Float) = 0.24
        UvRoot_Float("UvRoot", Vector) = (0.5, 0, 0, 0)
        WindDirection_Vector3("Wind Direction", Vector) = (1, 0, 0, 0)
        WindSymmetry_Float("Wind Symetry", Range(-1, 1)) = 0
        [NoScaleOffset]BaseColor("BaseColor", 2D) = "white" {}
        [NoScaleOffset]Smoothnes_Tex2D("Texture2D", 2D) = "white" {}
        [NoScaleOffset]NormalMap_Tex2D("Texture2D", 2D) = "bump" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue"="Geometry"
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

            // Directivos de unity para agregar lighting que podrian servir
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            // Defines
            #define _NORMALMAP 1
            
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
                float4 shadowCoord : TEXCOORD8;
            };

            float4 Smoothnes_Tex2D_TexelSize;
            float4 NormalMap_Tex2D_TexelSize;
            float4 BaseColor_TexelSize;
            float2 WindMovement_Vec2;
            float WindDensity_Float;
            float WindStrength_Float;
            float2 UvRoot_Float;
            float3 WindDirection_Vector3;
            float WindSymmetry_Float;
            
            TEXTURE2D(Smoothnes_Tex2D);
            SAMPLER(samplerSmoothnes_Tex2D);
            TEXTURE2D(NormalMap_Tex2D);
            SAMPLER(samplerNormalMap_Tex2D);
            TEXTURE2D(BaseColor);
            SAMPLER(samplerBaseColor);


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
            
            float3 ApplyWindForceToPosition(Attributes input)
            {

                float3 Normalized_WindDirection_Vector3 = normalize(WindDirection_Vector3);
            
                float UV_Distance_To_UV_Root_Leaf_Anchor = distance(input.uv0.xy, UvRoot_Float);
                
                float2 Speeded_Time = _TimeParameters.x.xx *  WindMovement_Vec2;
                
                float2 Random_UV_Coordinates_For_Gradient_Noise = TransformObjectToWorld(input.positionOS).xy * float2 (1, 1) + Speeded_Time;
            
                // El noise va de 0 a 1. La idea es que el viento simulado no sea unidireccional. Por eso  originalmente se le restaba .5
                // a el valor para que los movimientos oscilen de [-0.5, 0.5]. 
                float GradientNoise_Value;
                Unity_GradientNoise_float(Random_UV_Coordinates_For_Gradient_Noise, WindDensity_Float, GradientNoise_Value);
                // A la WindSimmetry le hacemos lo inverso (el input es de -1 a 1, para que sea mas entendible para el usuario....osea nadie)
                float Abs_WindSymmetry_Float = (WindSymmetry_Float / 2) + 0.5;

                // Al ruido ahora se le resta los valores de la simetria del viento y se establece el rango de valores que van a sumar o restar a los vertices
                float Modified_Noise_Range = GradientNoise_Value - Abs_WindSymmetry_Float;
                
                // La fuerza del viento depende de cuan lejos es la posicion de la textura de este vertice, de la textura UV_Root (el ancla de la hoja)
                float Wind_Strength_For_Vertex = UV_Distance_To_UV_Root_Leaf_Anchor * (Modified_Noise_Range * WindStrength_Float);

                // Distribui la fuerza del viento en las componentes xyz que correspondan al vector WindDirection
                float3 Directed_Wind_Vector = Normalized_WindDirection_Vector3.xyz * Wind_Strength_For_Vertex.xxx;
                
                return Directed_Wind_Vector + input.positionOS;
            }
            

            Varyings vert(Attributes input)
            {
                Varyings varyings = (Varyings)0;
            
                // Assign modified vertex attributes
                input.positionOS = ApplyWindForceToPosition(input);

                float3 positionWS = TransformObjectToWorld(input.positionOS);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                varyings.normalWS = normalWS;		
                varyings.positionCS = TransformWorldToHClip(positionWS);
                varyings.texCoord0 = input.uv0;
                varyings.viewDirectionWS = GetWorldSpaceViewDir(positionWS);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                varyings.shadowCoord = GetShadowCoord(vertexInput);

                return varyings;
            }

            half4 frag(Varyings varyings) : SV_TARGET
            {
                
                // transposed multiplication by inverse matrix to handle normal scale
                float3 objSpaceNormal =           normalize(mul(normalize(varyings.normalWS).xyz, (float3x3) UNITY_MATRIX_M));      
                
                // Logica del frag shader particular a las hojas
                UnityTexture2D BaseColor_Tex2D = UnityBuildTexture2DStructNoScale(BaseColor);
                float4 BaseColor_Tex2d_Sampled = SAMPLE_TEXTURE2D(BaseColor_Tex2D.tex, BaseColor_Tex2D.samplerstate, varyings.texCoord0);
                float4 NormalMap_Tex2d_Sampled = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(NormalMap_Tex2D).tex, UnityBuildTexture2DStructNoScale(NormalMap_Tex2D).samplerstate, varyings.texCoord0);
                NormalMap_Tex2d_Sampled.rgb = UnpackNormalRGB(NormalMap_Tex2d_Sampled);
                float3 NormalBlend = SafeNormalize(float3(NormalMap_Tex2d_Sampled.rg + objSpaceNormal.rg, NormalMap_Tex2d_Sampled.b * objSpaceNormal.b));
                float4 Smoothnes_Tex2D_Sampled = SAMPLE_TEXTURE2D(UnityBuildTexture2DStructNoScale(Smoothnes_Tex2D).tex, UnityBuildTexture2DStructNoScale(Smoothnes_Tex2D).samplerstate, varyings.texCoord0);
        
                // Este codigo esta mega copiado de esta libreria "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                // Este codigo general corresponde a Physically Based Materials, la abstraccion mas general de shaders para el render pipeline de unity
                // Por integridad academica lo lei, y borre llamadas inecesarias a funcionalidades que no usamos aca, asi entiendo mas de como funciona
                float bakedGI = SAMPLE_GI(float2(0,0), 0, varyings.normalWS);
                float smoothness = saturate((Smoothnes_Tex2D_Sampled).x);
                float occlusion = 0.14;
                BRDFData brdfData;
                brdfData.diffuse = BaseColor_Tex2d_Sampled.xyz;
                brdfData.specular = 0;
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
       
    }
    // Este shader es identico al anterior exceptuando que no define valores para el color. Simplemente contempla el movimiento de los vertices
    // Es mas magia de unity en el que este Pass (otra ejecucion), por convencion del Tag "Shadow Caster", se interpreta que los resultados
    // se van a usar para crear un shadow map. Investigue un poco de Shadow Maps pero, a esta altura, le confie a unity que haga lo que quiera
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
            "UniversalMaterialType" = "Lit"
            "Queue"="Geometry"
        }
        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }

        
        HLSLPROGRAM

        // Pragmas
        #pragma target 2.0
        #pragma only_renderers gles gles3 glcore d3d11
        #pragma multi_compile_instancing
        #pragma vertex vert
        #pragma fragment frag

        // Includes generales, hace falta este para acceder a la property de Lighting
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        // --------------------------------------------------
        // Structs
        struct Attributes
        {
            float3 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float4 uv0 : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS : SV_POSITION;
        };

        // Uniforms del shader
        CBUFFER_START(UnityPerMaterial)
        float4 Smoothnes_Tex2D_TexelSize;
        float4 NormalMap_Tex2D_TexelSize;
        float4 BaseColor_TexelSize;
        float2 WindMovement_Vec2;
        float WindDensity_Float;
        float WindStrength_Float;
        float2 UvRoot_Float;
        float3 WindDirection_Vector3;
        
        float WindSymmetry_Float;
        CBUFFER_END
            
        void Unity_Normalize_float3(float3 In, out float3 Out)
        {
            Out = normalize(In);
        }

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


        float3 ApplyWindForceToPosition(Attributes input) {
            
            float3 Normalized_WindDirection_Vector3 = normalize(WindDirection_Vector3);
           
            float UV_Distance_To_UV_Root_Leaf_Anchor = distance(input.uv0.xy, UvRoot_Float);
            
            float2 Speeded_Time = _TimeParameters.x.xx *  WindMovement_Vec2;
            
            float2 Random_UV_Coordinates_For_Gradient_Noise = TransformObjectToWorld(input.positionOS).xy * float2 (1, 1) + Speeded_Time;
        
            // El noise va de 0 a 1. La idea es que el viento simulado no sea unidireccional. Por eso  originalmente se le restaba .5
            // a el valor para que los movimientos oscilen de [-0.5, 0.5]. 
            float GradientNoise_Value;
            Unity_GradientNoise_float(Random_UV_Coordinates_For_Gradient_Noise, WindDensity_Float, GradientNoise_Value);
            // A la WindSimmetry le hacemos lo inverso (el input es de -1 a 1, para que sea mas entendible para el usuario....osea nadie)
            float Abs_WindSymmetry_Float = (WindSymmetry_Float / 2) + 0.5;

            // Al ruido ahora se le resta los valores de la simetria del viento y se establece el rango de valores que van a sumar o restar a los vertices
            float Modified_Noise_Range = GradientNoise_Value - Abs_WindSymmetry_Float;
            
            // La fuerza del viento depende de cuan lejos es la posicion de la textura de este vertice, de la textura UV_Root (el ancla de la hoja)
            float Wind_Strength_For_Vertex = UV_Distance_To_UV_Root_Leaf_Anchor * (Modified_Noise_Range * WindStrength_Float);

            // Distribui la fuerza del viento en las componentes xyz que correspondan al vector WindDirection
            float3 Directed_Wind_Vector = Normalized_WindDirection_Vector3.xyz * Wind_Strength_For_Vertex.xxx;
            
            return Directed_Wind_Vector + input.positionOS;
        }

        // Estas variables se pueden definir en el Shader y sus valores son inyectados directamente por Unity. Lo mismo suele ser
        // para matrices de transformacion por ejemplo
        float3 _LightDirection;
        
        Varyings vert(Attributes input)
        {
            Varyings output = (Varyings)0;

            // Returns the camera relative position (if enabled)
            float3 positionWS = TransformObjectToWorld(ApplyWindForceToPosition(input));
            float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
            float4 tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w);

            // Bueno esto es un poco de magia de unity que fui robando de las librerias que tienen
            float invNdotL = 1.0 - saturate(dot(_LightDirection, normalWS));
            float scale = invNdotL * _ShadowBias.y;

            // normal bias is negative since we want to apply an inset normal offset
            positionWS = _LightDirection * _ShadowBias.xxx + positionWS;
            positionWS = normalWS * scale.xxx + positionWS;

            output.positionCS = mul(UNITY_MATRIX_VP, float4(positionWS, 1.0));
            return output;
        }

        half4 frag(Varyings varyings) : SV_TARGET 
        {  
            return 0;
        }
            ENDHLSL
        }
        
    }
    
    FallBack "Hidden/Shader Graph/FallbackError"
}