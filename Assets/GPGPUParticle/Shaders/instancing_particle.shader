Shader "Custom/InstancingParticle"
{
    Properties
    {

        [HDR]_Color("Color", Color) = (1,1,1,1)
        [HDR] _EndColor("EndColor", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        [KeywordEnum(RT,CB)]
        _BUFTYPE("BufferType", float) = 0

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
            #pragma shader_feature _BUFTYPE_RT _BUFTYPE_CB 
            
            sampler2D _MainTex;

            struct Input
            {
                float2 uv_MainTex;
                float4 color;
                float alpha;
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
            StructuredBuffer<float3> beamTopBuffer;
            StructuredBuffer<float2> lifeBuffer;
            #endif



            void setup() {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                //float3 p = positionBuffer[unity_InstanceID] * 10.0;
                #endif
            }

            int InstanceNum;
            float4 textureSize;
            sampler2D positionRenderTexture;
            int particleNumPerEmitter;
            int culcType;//0 : RT, 1 * ComBuffer 
            
            float4 _Color;
            float4 _EndColor;
            void vert(inout appdata v, out Input o) {
                UNITY_INITIALIZE_OUTPUT(Input, o);
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                
                //emit点は同じだからuvをサイクルさせる
                
                //culcType = 0.0;
                float id = 0.0;
                float4 position = float4(0.0, 0.0, 0.0, 0.0);
                int instanceID = 0;
                
                #ifdef _BUFTYPE_RT
                instanceID = unity_InstanceID;
                id = fmod(instanceID, InstanceNum);
                float uvx = id  / textureSize.x;
                float2 delta = float2(1.0, 1.0) / textureSize;
                delta /= 2.0;
                float4 uv = float4(uvx+delta.x, delta.y, 0.0, 0.0);
                position = tex2Dlod(positionRenderTexture, float4(uv));
                
                #elif _BUFTYPE_CB
                instanceID = unity_InstanceID;
                id = fmod(unity_InstanceID, InstanceNum);
                position = float4(0.0, 0.0, 0.0, 1.0);
                position.xyz =  beamTopBuffer[id].xyz;
                
                #endif
                
                
                //サイクルさせる前のidを使う
                float3 noise = curlNoise(float4(position.x, instanceID*10.0, instanceID*10.0, _Time.y*1.0));
                
                
                float2 life = lifeBuffer[(int)id];
                position.xyz += noise * (0.45 + ((life.x - life.y) / life.x)  * sin(_Time.y + id * 10.0)) * pow((life.x - life.y)/life.x, 1.3);
                //position.xyz += noise * 0.45 * life.y;
                float scaleOff = 0.05 * (pow((life.x - life.y) / life.x, 1.5));
                float a = (life.x - life.y)/life.x;
                o.color = lerp(_Color, _EndColor, a);
                o.alpha = 1.0-a;
               
                float4  vertex = mul(ScaleMatrix(float3(scaleOff, scaleOff, scaleOff)), v.vertex);
                v.vertex = mul(TranslateMatrix(position * 1.0), vertex);
                
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
                if(length(c) < 0.95){
                    discard;
                }
                o.Albedo = c.rgb;
                o.Alpha = c.a;
            }
            ENDCG
        }
            FallBack "Diffuse"
}