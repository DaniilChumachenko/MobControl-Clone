Shader "MobControl/Grass Ground"
{
    Properties
    {
        _SoilTex ("Soil", 2D) = "white" {}
        _LeafTex ("Grass Variation", 2D) = "white" {}
        _WorldTiling ("World Tiling", Float) = 0.18
        _GrassCoverage ("Grass Coverage", Range(0, 1)) = 0.82
        _GrassDark ("Grass Dark", Color) = (0.055, 0.16, 0.025, 1)
        _GrassLight ("Grass Light", Color) = (0.32, 0.58, 0.09, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _SoilTex;
        sampler2D _LeafTex;
        half _WorldTiling;
        half _GrassCoverage;
        fixed4 _GrassDark;
        fixed4 _GrassLight;

        struct Input
        {
            float3 worldPos;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 soilUv = IN.worldPos.xz * _WorldTiling;
            float2 leafUvA = IN.worldPos.xz * (_WorldTiling * 0.31h);
            float2 leafUvB = IN.worldPos.zx * (_WorldTiling * 0.19h) + float2(0.37h, 0.61h);

            fixed3 soil = tex2D(_SoilTex, soilUv).rgb;
            fixed3 leafA = tex2D(_LeafTex, leafUvA).rgb;
            fixed3 leafB = tex2D(_LeafTex, leafUvB).rgb;
            half variation = saturate((leafA.g + leafB.g) * 0.42h + soil.r * 0.24h);
            fixed3 grass = lerp(_GrassDark.rgb, _GrassLight.rgb, variation);
            grass *= lerp(0.78h, 1.12h, soil.r);

            o.Albedo = lerp(soil * 0.68h, grass, _GrassCoverage);
            o.Metallic = 0;
            o.Smoothness = 0.08h;
            o.Occlusion = 0.9h;
            o.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
