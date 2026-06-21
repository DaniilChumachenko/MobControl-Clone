Shader "MobControl/Roman Siege Tower PBR"
{
    Properties
    {
        _MainTex ("Albedo", 2D) = "white" {}
        _MetallicTex ("Metallic", 2D) = "black" {}
        _RoughnessTex ("Roughness", 2D) = "white" {}
        _BumpMap ("Normal", 2D) = "bump" {}
        _EmissionMap ("Emission", 2D) = "black" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1
        _EmissionStrength ("Emission Strength", Range(0, 2)) = 0.35
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _MetallicTex;
        sampler2D _RoughnessTex;
        sampler2D _BumpMap;
        sampler2D _EmissionMap;
        half _NormalStrength;
        half _EmissionStrength;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 albedo = tex2D(_MainTex, IN.uv_MainTex);
            half metallic = tex2D(_MetallicTex, IN.uv_MainTex).r;
            half roughness = tex2D(_RoughnessTex, IN.uv_MainTex).r;
            fixed3 emission = tex2D(_EmissionMap, IN.uv_MainTex).rgb;

            o.Albedo = albedo.rgb;
            o.Metallic = metallic;
            o.Smoothness = 1.0h - roughness;
            o.Normal = UnpackScaleNormal(tex2D(_BumpMap, IN.uv_MainTex), _NormalStrength);
            o.Emission = emission * _EmissionStrength;
            o.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Standard"
}
