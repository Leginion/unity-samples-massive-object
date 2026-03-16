Shader "Unlit/InstancedUnlitIndirect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color Tint", Color) = (1,1,1,1) // optional: base color
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma target 4.5 // support StructuredBuffer
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #pragma instancing_options procedural:setup // enable indirect
            #pragma multi_compile_instancing // compatible

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            
            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                StructuredBuffer<float4x4> instanceMatrices; // same name with C# buffer field
            #endif
            
            void setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                unity_ObjectToWorld = instanceMatrices[unity_InstanceID];
            
                unity_WorldToObject = unity_ObjectToWorld;
                unity_WorldToObject._14_24_34 *= -1; // reverse matrix (unity requires)
            
                // unity_WorldToObject._11_22_33 = 1.0 / unity_WorldToObject._11_22_33; // reverse scaling
            
                float3 scale = float3(
                    length(unity_WorldToObject._11_21_31),
                    length(unity_WorldToObject._12_22_32),
                    length(unity_WorldToObject._13_23_33)
                );
                unity_WorldToObject._11_22_33 = scale > 0.0001 ? 1.0 / scale : 1.0;
                #endif
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                // UNITY_INSTANCING_ENABLED
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                v.normal = mul((float3x3)unity_ObjectToWorld, v.normal);
                #endif
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
