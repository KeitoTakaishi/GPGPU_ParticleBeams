Shader "Custom/instancing_spark"
{
    Properties
    {

        [HDR] _Color("Color", Color) = (1,1,1,1)
        [HDR] _EndColor("EndColor", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        

    }
        SubShader
        {
            Tags {"Queue"="Transparent" "RenderType"="Transparent"}
            LOD 200
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma target 5.0
            #include "UnityCG.cginc"
            #include "utils.hlsl"
            #include "noise4d.hlsl"
            #pragma surface surf Standard alpha vertex:vert
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            
            
            sampler2D _MainTex;

            struct Input
            {
                float2 uv_MainTex;
                float4 color;
                //float3 worldPos;
            };

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent :TANGENT0;
                float2 texcoord : TEXCOORD0;
                float2 texcoord1 : TEXCOORD1;
                float2 texcoord2 : TEXCOORD2;
                uint instanceID : SV_InstanceID;
            };

            struct instanceData
            {
                float3 position;

            };

            #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
            StructuredBuffer<float3> positionBuffer;
            StructuredBuffer<float3> velocityBuffer;
            StructuredBuffer<float2> lifeBuffer;
            #endif



            void setup() {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                #endif
            }

            
            
            float4 _Color;
            float4 _EndColor;
            void vert(inout appdata v, out Input o) {
                UNITY_INITIALIZE_OUTPUT(Input, o);
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                
                
                int instanceID = unity_InstanceID;
                float4 position = float4(0.0, 0.0, 0.0, 1.0);
                position.xyz =  positionBuffer[instanceID].xyz;
                
                float2 life = lifeBuffer[instanceID].y;
                float scaleOff = 0.15 * (pow((life.x - life.y), 1.5) - 1.0);
                //float scaleOff = 0.4 * life.y;
                //float caleOff = 0.03;
                
                float4  vertex = mul(ScaleMatrix(float3(scaleOff, scaleOff, scaleOff)), v.vertex);
                v.vertex = mul(TranslateMatrix(position * 1.0), vertex);
                o.color = lerp(_Color, _EndColor, length(velocityBuffer[instanceID].xyz)/10.0);
            
                #endif
            }

            half _Glossiness;
            half _Metallic;
            
            int width, height;
            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * IN.color;
                //fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                if(length(c) < 1.0){
                    discard;
                }
                o.Albedo = c.rgb;
                o.Alpha = c.a;
            }
            ENDCG
        }
            FallBack "Diffuse"
}