Shader "Unlit/VertexOffset"
{
    Properties
    {
        //_Color ("Color", Color) = (1,1,1,1)
        _Scale ("UV Scale", Float) = 1
        _Offset ("UV Offset", Float) = 0
        _ColorA ("Color A", Color) = (1,1,1,1)
        _ColorB ("Color B", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { 
            "RenderType"="Opaque"
        }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            #define TAU 6.28318530718

            float4 _ColorA;
            float4 _ColorB;
            float _Scale;
            float _Offset;

            struct MeshData
            {
                float4 vertex : POSITION;
                float3 normals: NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Interpolators
            {
                float4 vertex : SV_POSITION;
                float3 normal: TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            Interpolators vert (MeshData v)
            {
                Interpolators o;

                float wave = cos((v.uv.y - _Time.y * 0.1 ) * TAU * 5);
                //v.vertex.y = wave;
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normals);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (Interpolators i) : SV_Target
            {
                float t = cos((i.uv.y  - _Time.y * 0.1 ) * TAU * 5) * 0.5 + 0.5;
                return t;
            }
            ENDCG
        }
    }
}
