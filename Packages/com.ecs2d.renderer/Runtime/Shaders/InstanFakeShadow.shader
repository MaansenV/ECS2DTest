Shader "Instanced/SpriteRendererIndexedUvFakeShadow" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.4)
        _ShadowOffsetX ("Shadow Offset X", Float) = 0.12
        _ShadowOffsetY ("Shadow Offset Y", Float) = -0.08
        _ShadowScaleX ("Shadow Scale X", Float) = 1.1
        _ShadowScaleY ("Shadow Scale Y", Float) = 0.35
        _ShadowSkewX ("Shadow Skew X", Float) = 0.35
        _ShadowSkewY ("Shadow Skew Y", Float) = 0
        _ShadowDepthBias ("Shadow Depth Bias", Float) = -0.0005
    }

    SubShader {
        Tags {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }
        Cull Back
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma exclude_renderers gles
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed _Cutoff;
            fixed4 _ShadowColor;
            float _ShadowOffsetX;
            float _ShadowOffsetY;
            float _ShadowScaleX;
            float _ShadowScaleY;
            float _ShadowSkewX;
            float _ShadowSkewY;
            float _ShadowDepthBias;

            StructuredBuffer<float4> translationAndRotationBuffer;
            StructuredBuffer<float2> scaleBuffer;
            StructuredBuffer<float4> uvBuffer;
            StructuredBuffer<int> frameIndexBuffer;
            StructuredBuffer<float2> flipBuffer;
            StructuredBuffer<float> renderDepthBuffer;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float4x4 rotationZMatrix(float zRotRadians) {
                float c = cos(zRotRadians);
                float s = sin(zRotRadians);
                return float4x4(
                    c,  s, 0,  0,
                    -s, c, 0,  0,
                    0,  0, 1,  0,
                    0,  0, 0,  1
                );
            }

            v2f vert(appdata_full v, uint instanceID : SV_InstanceID) {
                float4 translationAndRot = translationAndRotationBuffer[instanceID];
                float renderDepth = renderDepthBuffer[instanceID] + _ShadowDepthBias;
                int frameIndex = frameIndexBuffer[instanceID];
                float4 uv = uvBuffer[frameIndex];

                float2 scale = scaleBuffer[instanceID];
                float4 localVertex = v.vertex - float4(0.5, 0.5, 0, 0);
                localVertex.x *= scale.x;
                localVertex.y *= scale.y;

                localVertex.x *= _ShadowScaleX;
                localVertex.y *= _ShadowScaleY;

                float skewedX = localVertex.x + localVertex.y * _ShadowSkewX;
                float skewedY = localVertex.y + localVertex.x * _ShadowSkewY;
                localVertex.x = skewedX;
                localVertex.y = skewedY;

                float4 rotatedVertex = mul(localVertex, rotationZMatrix(translationAndRot.w));
                float3 worldPosition = translationAndRot.xyz + rotatedVertex.xyz;
                worldPosition.x += _ShadowOffsetX;
                worldPosition.y += _ShadowOffsetY;

                v2f o;
                o.pos = UnityObjectToClipPos(float4(worldPosition, 1.0f));
                float4 depthClipPos = UnityObjectToClipPos(float4(worldPosition.xy, renderDepth, 1.0f));
                o.pos.z = depthClipPos.z;

                o.uv = v.texcoord * uv.xy + uv.zw;

                float2 flip = flipBuffer[instanceID];
                float2 frameMin = uv.zw;
                float2 frameMax = uv.zw + uv.xy;
                if (flip.x > 0.5) o.uv.x = frameMax.x - (o.uv.x - frameMin.x);
                if (flip.y > 0.5) o.uv.y = frameMax.y - (o.uv.y - frameMin.y);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed sourceAlpha = tex2D(_MainTex, i.uv).a;
                clip(sourceAlpha - _Cutoff);

                fixed4 shadow = _ShadowColor;
                shadow.a *= sourceAlpha;
                return shadow;
            }
            ENDCG
        }
    }
}
