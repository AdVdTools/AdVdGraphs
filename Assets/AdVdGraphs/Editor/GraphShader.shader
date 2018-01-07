Shader "Hidden/AdVd/GraphShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1, 1, 1, 1)
		_MarkerSize ("MarkerSize", Vector) = (0.2, 0.2, 0, 0)
		_Transform ("Transform", Vector) = (0, 0, 1, 1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
		Cull Off
		LOD 100

		// Lines
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			
			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4 _Color;
			float4 _Transform;
			
			v2f vert (appdata v)
			{
				v2f o;
				float3 vertex = float3(
					_Transform.x + _Transform.z * v.vertex.x,
					_Transform.y + _Transform.w * v.vertex.y, 1);
				o.vertex = UnityObjectToClipPos(vertex);
				o.uv = float2(0, 0);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _Color;
				return col;
			}
			ENDCG
		}

		// Bars
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float4 _Color;
			float4 _Transform;
			
			v2f vert (appdata v)
			{
				v2f o;
				float3 vertex = float3(
					_Transform.x + _Transform.z * v.vertex.x,
					_Transform.y + _Transform.w * v.vertex.y, 1);
				o.vertex = UnityObjectToClipPos(vertex);
				o.uv = float2(0, 0);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _Color;
				return col;
			}
			ENDCG
		}

		// Area
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4 _Color;
			float4 _Transform;
			
			v2f vert (appdata v)
			{
				v2f o;
				float3 vertex = float3(
					_Transform.x + _Transform.z * v.vertex.x,
					_Transform.y + _Transform.w * v.vertex.y, 1);
				o.vertex = UnityObjectToClipPos(vertex);
				o.uv = float2(0, 0);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _Color;
				col.a *= 0.25;
				return col;
			}
			ENDCG
		}

		// Markers
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float4 _Color;
			float4 _MarkerSize;
			float4 _Transform;
			
			v2f vert (appdata v)
			{
				v2f o;

				float3 offset = float3(-0.5 + v.uv.x, -0.5 + v.uv.y, 0) * 0.2;
				
				float3 vertex = float3(
					_Transform.x + _Transform.z * v.vertex.x,
					_Transform.y + _Transform.w * v.vertex.y, 1);
				o.vertex = UnityObjectToClipPos(vertex);
				o.vertex.x += offset.x * _MarkerSize.x;
				o.vertex.y += offset.y * _MarkerSize.y;
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = _Color * tex2D(_MainTex, i.uv);
				return col;
			}
			ENDCG
		}
	}
}
