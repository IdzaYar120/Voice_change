// Flat anime sky gradient with a hard-edged sun.
Shader "AnimeSouls/Sky"
{
    Properties
    {
        _Top ("Top", Color) = (0.24,0.52,0.95,1)
        _Horizon ("Horizon", Color) = (0.78,0.9,1,1)
        _Bottom ("Bottom", Color) = (0.55,0.7,0.85,1)
        _SunColor ("Sun", Color) = (1,0.97,0.85,1)
        _SunSize ("Sun Size", Range(0.001,0.1)) = 0.012
    }
    SubShader
    {
        Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _Top, _Horizon, _Bottom, _SunColor;
            half _SunSize;

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; float3 dir : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;
                fixed3 col = h > 0 ? lerp(_Horizon.rgb, _Top.rgb, pow(saturate(h), 0.55))
                                   : lerp(_Horizon.rgb, _Bottom.rgb, pow(saturate(-h), 0.35));
                float s = dot(d, normalize(_WorldSpaceLightPos0.xyz));
                col = lerp(col, _SunColor.rgb, smoothstep(1 - _SunSize, 1 - _SunSize * 0.7, s));
                col += _SunColor.rgb * pow(saturate(s), 64) * 0.25;
                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
