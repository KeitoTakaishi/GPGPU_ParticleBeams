Shader "Custom/SimpleInstancing"
{
    Properties
    {

        [HDR]_Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}

    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 200

            CGPROGRAM
            #pragma target 5.0
            #include "UnityCG.cginc"
            #include "utils.hlsl"
            #pragma surface surf Standard fullforwardshadows vertex:vert
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            sampler2D _MainTex;

            struct Input
            {
                float2 uv_MainTex;
                float3 worldPos;
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
            //StructuredBuffer<float3> positionBuffer;
            //StructuredBuffer<float3> normalBuffer;
            StructuredBuffer<float3> lifeBuffer;
            #endif



            void setup() {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                //float3 p = positionBuffer[unity_InstanceID] * 10.0;
                #endif
            }

            int InstanceNum;
            float4 textureSize;
            sampler2D positionRenderTexture;

            void vert(inout appdata v, out Input o) {
                UNITY_INITIALIZE_OUTPUT(Input, o);
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                /*
                //RenderTextureを送信するパターン
                float id = fmod(unity_InstanceID, InstanceNum);
                float uvx = (float)(id) / (float)(InstanceNum);
                float4 _uv = float4(uvx, 0.0, 0.0, 0.0);
                float3 position = tex2Dlod(positionRenderTexture, float4(_uv)).xyz;

                //行列演算出ないと座標変換できない
                id *= 10.0;
                float scaleOff = 0.1* frac(id / 100.0);
                float4  vertex = mul(ScaleMatrix(float3(scaleOff, scaleOff, scaleOff)), v.vertex);
                vertex = mul(RotYMatrix(_Time.y * 10.0 + id), vertex);
                vertex = mul(RotXMatrix(_Time.y * 10.0 + id), vertex);
                vertex = mul(RotZMatrix(_Time.y * 10.0 + id),vertex);
                v.vertex = mul(TranslateMatrix(position * 1.0), vertex);
                o.worldPos = position;
                */

                
                float id = fmod(unity_InstanceID, InstanceNum);
                //float uvx = (float)(id) / (float)(InstanceNum);
                float uvx = (float)(fmod(id,textureSize.x));
                uvx = uvx / (float)(textureSize.x-1.0);
                float uvy = (float)(id) / (float)(textureSize.x);
                uvy = uvy / (float)(textureSize.y-1.0);
                
                
                
                //float uvx = (float)(id) / (float)(textureSize.y);
                
                //float4 uv = float4(float(fmod(id, textureSize.x)), 0.0, 0.0, 0.0);
                //uv.xy /= textureSize.xy;
                float4 uv = float4(uvx, uvy, 0.0, 0.0);
                uv = float4(id, 0.0, 0.0, 0.0);
                float3 position = tex2Dlod(positionRenderTexture, float4(uv)).xyz;
                
                float scaleOff = 0.2;
                //if (lifeBuffer[id].y > 0.01){
                //    scaleOff = lifeBuffer[id].y;
                //}else{
                    //scaleOff = 0.0;
                //}
            
                float4  vertex = mul(ScaleMatrix(float3(scaleOff, scaleOff, scaleOff)), v.vertex);
                v.vertex = mul(TranslateMatrix(position * 1.0), vertex);
                
                //o.worldPos = position;
                //v.vertex.y += 1.0; 
                //float3 position = positionBuffer[id];
                
                //float scaleOff = 0.05;
                //float4  vertex = mul(ScaleMatrix(float3(scaleOff, scaleOff, scaleOff)), v.vertex);
                //vertex = mul(RotYMatrix(_Time.y * 10.0 + id), vertex);
                //vertex = mul(RotXMatrix(_Time.y * 10.0 + id), vertex);
                //vertex = mul(RotZMatrix(_Time.y * 10.0 + id), vertex);
                //v.vertex = mul(TranslateMatrix(position * 1.0), vertex);
                //o.worldPos = position;
                

                #endif
            }

            half _Glossiness;
            half _Metallic;
            fixed4 _Color;
            int width, height;
            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            void surf(Input IN, inout SurfaceOutputStandard o)
            {
                
                //fixed4 c = fixed4(1.0, 1.0, 1.0, 1.0) * _Color * frac(_Time.y + len) *  2.0 * frac(sin(_Time.y));
                //fixed4 c = fixed4(1.0, 1.0, 1.0, 1.0) ;
                fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
                if(length(c) < 0.5){
                    discard;
                }
                o.Albedo = c.rgb;
                o.Alpha = c.a;
            }
            ENDCG
        }
            FallBack "Diffuse"
}