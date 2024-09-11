Shader "Custom/ClippingShader"
{
    Properties
    {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _ClipPos ("Clip Position", Vector) = (0,0,0,0)
        _ClipRange ("Clip Range", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma exclude_renderers gles xbox360
            
            #include "UnityCG.cginc"
            
            struct appdata_t
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            sampler2D _MainTex;
            float4 _ClipPos;
            float _ClipRange;
            
            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float3 worldPos = mul(unity_ObjectToWorld, float4(i.pos.xy, 0, 1)).xyz;
                float distance = length(worldPos - _ClipPos.xyz);
                if (distance > _ClipRange)
                {
                    discard; // Discard fragments outside the clipping range
                }
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}