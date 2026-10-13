// Cel-shaded, vertex-coloured material without outline. Used for the generated terrain.
Shader "AnimeSouls/ToonTerrain"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _ShadowTint ("Shadow Tint", Color) = (0.6,0.62,0.82,1)
        _RampThreshold ("Ramp Threshold", Range(0,1)) = 0.46
        _RampSmooth ("Ramp Smooth", Range(0.001,0.5)) = 0.06
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }

        CGPROGRAM
        #pragma surface surf Toon fullforwardshadows vertex:vert
        #pragma target 3.0

        fixed4 _Color;
        fixed4 _ShadowTint;
        half _RampThreshold;
        half _RampSmooth;

        struct Input { float4 vcol; };

        void vert (inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.vcol = v.color;
        }

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
            o.Albedo = _Color.rgb * IN.vcol.rgb;
            o.Alpha = 1;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
