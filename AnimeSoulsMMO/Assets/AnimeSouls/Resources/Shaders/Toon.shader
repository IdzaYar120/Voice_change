// Cel-shaded material with an inverted-hull outline, used for characters and props.
Shader "AnimeSouls/Toon"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _ShadowTint ("Shadow Tint", Color) = (0.62,0.6,0.8,1)
        _RampThreshold ("Ramp Threshold", Range(0,1)) = 0.48
        _RampSmooth ("Ramp Smooth", Range(0.001,0.5)) = 0.03
        _RimColor ("Rim Color", Color) = (1,1,1,0.35)
        _RimPower ("Rim Power", Range(0.5,8)) = 3.5
        _OutlineColor ("Outline Color", Color) = (0.07,0.05,0.1,1)
        _OutlineWidth ("Outline Width", Range(0,0.02)) = 0.0035
        _Emission ("Emission", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            float _OutlineWidth;
            fixed4 _OutlineColor;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; UNITY_FOG_COORDS(0) };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 n = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                float2 off = TransformViewToProjection(n.xy);
                float len = length(off);
                off = len > 1e-5 ? off / len : float2(0, 0);
                // Constant screen-space thickness, slightly thinner far away.
                o.pos.xy += off * min(o.pos.w, 25.0) * _OutlineWidth;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 c = _OutlineColor;
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }

        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows
        #pragma target 3.0

        fixed4 _Color;
        fixed4 _ShadowTint;
        fixed4 _RimColor;
        half _RampThreshold;
        half _RampSmooth;
        half _RimPower;
        half _Emission;

        struct Input { float3 viewDir; };

        half4 LightingToon (SurfaceOutput s, half3 lightDir, half atten)
        {
            half ndl = dot(s.Normal, lightDir) * 0.5 + 0.5;
            half lit = smoothstep(_RampThreshold - _RampSmooth, _RampThreshold + _RampSmooth, ndl);
            lit = min(lit, smoothstep(0.35, 0.65, atten));
            half3 col = s.Albedo * _LightColor0.rgb * lerp(_ShadowTint.rgb, half3(1, 1, 1), lit);
            return half4(col, s.Alpha);
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            o.Albedo = _Color.rgb;
            half rim = 1 - saturate(dot(normalize(IN.viewDir), o.Normal));
            rim = smoothstep(0.55, 0.75, pow(rim, _RimPower * 0.5));
            o.Emission = _RimColor.rgb * _RimColor.a * rim + _Color.rgb * _Emission;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
