﻿// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/TriPlanarClearDiceFace"
{
    Properties
    {
        _Color("", Color) = (1, 1, 1, 1)
        _MainTex("", 2D) = "white" {}

        _Glossiness("", Range(0, 1)) = 0.5
        [Gamma] _Metallic("", Range(0, 1)) = 0

        _BumpScale("", Float) = 1
        _BumpMap("", 2D) = "bump" {}

        _MapScale("", Float) = 1

        _NumberColor ("Number Color", Color) = (1,1,1,1)

        _GlowTex ("Glow Tex", 2D) = "white" {}
        _GlowColor ("Glow Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Pass
        {
            Tags
            {
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
                "RenderType" = "Transparent"
                "PreviewType" = "Plane"
            }

            Cull Off
            Lighting Off
            ZWrite Off
            Blend One OneMinusSrcAlpha
            ColorMask A

            CGPROGRAM
             #pragma vertex vert
             #pragma fragment frag

             #include "UnityCG.cginc"

             float4 _Color;

             struct v2f {
                 float4  pos : SV_POSITION;
             };

             v2f vert(appdata_base v)
             {
                 v2f o;
                 o.pos = UnityObjectToClipPos(v.vertex);
                 return o;
             }

             half4 frag(v2f i) : COLOR
             {
                 return _Color;
             }
             ENDCG

        }
            
        Tags { "Queue"="Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off
        ColorMask RGBA
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard vertex:vert fullforwardshadows nolightmap alpha:premul
                 //finalcolor:writeAlpha

        #pragma target 3.0

        sampler2D _MainTex;

        half _BumpScale;
        sampler2D _BumpMap;

        half _MapScale;

        fixed4 _NumberColor;

        sampler2D _GlowTex;
        fixed4 _GlowColor;

        struct Input
        {
            float2 uv_MainTex;
            float3 localCoord;
            float3 localNormal;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void vert(inout appdata_full v, out Input data)
        {
            UNITY_INITIALIZE_OUTPUT(Input, data);
            data.localCoord = v.vertex.xyz;
            data.localNormal = v.normal.xyz;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Blending factor of triplanar mapping
            float3 bf = normalize(abs(IN.localNormal));
            bf /= dot(bf, (float3)1);

            float3 localCoord = IN.localCoord * _MapScale;

            // Triplanar mapping
            float2 tx = localCoord.yz;
            float2 ty = localCoord.zx;
            float2 tz = localCoord.xy;

            // Base color
            half4 color = _Color;
            if (bf.x > bf.y)
            {
                if (bf.x > bf.z)
                {
                    color *= tex2D(_MainTex, tx);
                }
                else
                {
                    color *= tex2D(_MainTex, tz);
                }
            }
            else
            {
                if (bf.y > bf.z)
                {
                    color *= tex2D(_MainTex, ty);
                }
                else
                {
                    color *= tex2D(_MainTex, tz);
                }
            }
            //half4 cx = tex2D(_MainTex, tx) * bf.x;
            //half4 cy = tex2D(_MainTex, ty) * bf.y;
            //half4 cz = tex2D(_MainTex, tz) * bf.z;
            //half4 color = (cx + cy + cz) * _Color;

            // Normal map
            half4 nx = tex2D(_BumpMap, tx) * bf.x;
            half4 ny = tex2D(_BumpMap, ty) * bf.y;
            half4 nz = tex2D(_BumpMap, tz) * bf.z;
            o.Normal = UnpackScaleNormal(nx + ny + nz, _BumpScale);

            // Misc parameters
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            float4 glowMask = tex2D(_GlowTex, IN.uv_MainTex);
            float glowStrength = dot(_GlowColor.rgb, float3(0.299f, 0.587f, 0.114f));
            //float numberGlow = glowStrength * glow

            float numberStrength = glowMask.r * (1.0f - glowStrength) * _NumberColor.a;

            o.Emission = _GlowColor * glowMask.a;
            o.Albedo = _NumberColor.rgb * numberStrength + color.rgb * (1.0f - numberStrength);

            o.Alpha = _Color.a;
        }

        void writeAlpha(Input IN, SurfaceOutputStandard o, inout fixed4 color)
        {
            color = float4(1,1,0,1);
        }
        ENDCG
    }
    FallBack "Diffuse"
}
