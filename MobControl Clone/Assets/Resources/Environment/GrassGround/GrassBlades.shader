Shader "MobControl/Grass Blades"
{
    Properties
    {
        _MainTex ("Leaf Texture", 2D) = "white" {}
        _Tint ("Grass Tint", Color) = (0.33, 0.68, 0.12, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.12
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 180
        Cull Off

        CGPROGRAM
        #pragma surface surf Standard addshadow
        #pragma target 3.0
        #pragma multi_compile_instancing

        sampler2D _MainTex;
        fixed4 _Tint;
        half _Cutoff;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 leaf = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = leaf.rgb * _Tint.rgb;
            o.Alpha = 1;
            o.Metallic = 0;
            o.Smoothness = 0.12;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
