Shader "Hidden/AdVd/GraphShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1, 1, 1, 1)
		_MarkerSize ("MarkerSize", Vector) = (0.2, 0.2, 0, 0)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull Off
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4 _Color;
			StructuredBuffer<float2> buffer;
			
			v2f vert (uint id: SV_VertexID)
			{
				v2f o;
				float3 vertex = float3(buffer[id].x, buffer[id].y, 1);
				o.vertex = UnityObjectToClipPos(vertex);
				o.uv = float2(0, 0);//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = _Color;// tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4 _Color;
			StructuredBuffer<float2> buffer;
			
			v2f vert (uint id: SV_VertexID)
			{
				v2f o;
				uint i = id / 2;
				float3 vertex = float3(buffer[i].x, buffer[i].y * ((id % 2) == 0), 1);
				o.vertex = UnityObjectToClipPos(vertex);
				o.uv = float2(0, 0);//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = _Color;// tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4 _Color;
			StructuredBuffer<float2> buffer;
			
			v2f vert (uint id: SV_VertexID)
			{
				v2f o;
				uint next = ((id + 1) % 6) / 3;
				uint zero = (id % 2);
				uint i = id / 6 + next;
				
				float3 vertex = float3(buffer[i].x, buffer[i].y * (1 - zero), 1);
				o.vertex = UnityObjectToClipPos(vertex);
				o.uv = float2(next, 1 - zero);//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = _Color;// * tex2D(_MainTex, i.uv);
				col.a *= 0.25;
				return col;
			}
			ENDCG
		}

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			
			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4 _Color;
			float4 _MarkerSize;
			StructuredBuffer<float2> buffer;
			
			v2f vert (uint id: SV_VertexID)
			{
				v2f o;
				uint next = ((id + 1) % 6) / 3;
				uint zero = (id % 2);
				uint i = id / 6;

				float3 offset = float3(-0.5 + next, -0.5 + (id % 2), 0) * 0.2;
				
				float3 vertex = float3(buffer[i].x, buffer[i].y, 1);
				o.vertex = UnityObjectToClipPos(vertex);
				o.vertex.x += offset.x * _MarkerSize.x;
				o.vertex.y += offset.y * _MarkerSize.y;
				o.uv = float2(next, 1 - zero);//o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = _Color * tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}
	}
}
