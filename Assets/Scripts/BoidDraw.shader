Shader "Unlit/BoidDraw"
{
    Properties
    {
		_Specular("SpecularColor", Color) = (1, 1, 1, 1)
		_Gloss("Gloss", Range(8.0, 256)) = 20
		_Diffuse("Diffuse", Color) = (1, 1, 1, 1)
		_AmbientWeight("AmbientWeight", float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#pragma target 4.5

            #include "UnityCG.cginc"
			#include "Lighting.cginc"

			fixed4 _Diffuse;
			float _AmbientWeight;
			float _Gloss;
			fixed4 _Specular;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 Normal : NORMAL;
            };

            struct v2f
            {
                float3 worldNormal :TEXCOORD0;
				float3 worldPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

			#if SHADER_TARGET >=45
				struct BoidData{
					float3 position;
					float4 rotation;

					float4 flockRot;
					float3 separationHeading;
					float3 flockCectre;
					float4x4 TRSMatrix;
				};
				StructuredBuffer<BoidData> boidsBuffer;
				float4 scale;
			#endif


            v2f vert (appdata v, uint id: SV_INSTANCEID)
            {
                v2f o;
				#if SHADER_TARGET >=45
				v.vertex = mul(boidsBuffer[id].TRSMatrix, v.vertex);
				#endif

                o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldNormal = UnityObjectToWorldNormal(v.Normal);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz * _AmbientWeight;
				fixed3 worldNormal = normalize(i.worldNormal);
				fixed3 worldLightDir = normalize(_WorldSpaceLightPos0.xyz);
				fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * saturate(dot(worldNormal, worldLightDir));
				fixed3 viewDir = normalize(UnityWorldSpaceViewDir(i.worldPos));
				fixed3 halfDir = normalize(worldLightDir + viewDir);
				fixed3 specular = _LightColor0.rgb * _Specular.rgb * pow(max(0, dot(viewDir, halfDir)), _Gloss);

				fixed3 color = ambient + diffuse + specular;

                return fixed4(color, 1.0);
            }
            ENDCG
        }
    }
}
