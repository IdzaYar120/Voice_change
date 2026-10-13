// Unlit energy material: white-hot core facing the camera, saturated colour at the rim.
Shader "AnimeSouls/Glow"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _Core ("Core Whiteness", Range(0,1)) = 0.65
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Color;
            half _Core;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float3 n : TEXCOORD0; float3 v : TEXCOORD1; fixed4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.n = UnityObjectToWorldNormal(v.normal);
                o.v = WorldSpaceViewDir(v.vertex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float nl = length(i.n);
                float facing = nl > 1e-4 ? abs(dot(i.n / nl, normalize(i.v))) : 0;
                float core = smoothstep(0.55, 0.95, facing) * _Core;
                fixed4 c = _Color * i.color;
                c.rgb = lerp(c.rgb, fixed3(1, 1, 1), core);
                c.a *= lerp(1.0, 0.8, core);
                return c;
            }
            ENDCG
        }
    }
}
