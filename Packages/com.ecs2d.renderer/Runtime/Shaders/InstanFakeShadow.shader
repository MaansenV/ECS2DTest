Shader "Instanced/SpriteRendererIndexedUvFakeShadow" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.4)
        _ShadowLocalOffsetX ("Shadow Local Offset X", Float) = 0
        _ShadowLocalOffsetY ("Shadow Local Offset Y", Float) = -0.18
        _ShadowOffsetX ("Shadow Offset X (Legacy)", Float) = 0
        _ShadowOffsetY ("Shadow Offset Y (Legacy)", Float) = 0
        _ShadowScaleX ("Shadow Scale X", Float) = 1.1
        _ShadowScaleY ("Shadow Scale Y", Float) = 0.35
        _ShadowSkewX ("Shadow Skew X", Float) = 0.35
        _ShadowSkewY ("Shadow Skew Y", Float) = 0
        _ShadowAnchorMode ("Shadow Anchor Mode", Range(0,1)) = 0
        _ShadowAnchorPivot ("Shadow Anchor Pivot", Vector) = (0, -0.5, 0, 0)
        _ShadowDepthBias ("Shadow Depth Bias", Float) = 0.0005
        _ShadowBlurStrength ("Shadow Blur Strength", Range(0,4)) = 0
    }

    SubShader {
        Tags {
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
        }
        Cull Back
        Lighting Off
        ZWrite Off
        ZTest LEqual
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
            float4 _MainTex_TexelSize;
            float _ShadowLocalOffsetX;
            float _ShadowLocalOffsetY;
            float _ShadowOffsetX;
            float _ShadowOffsetY;
            float _ShadowScaleX;
            float _ShadowScaleY;
            float _ShadowSkewX;
            float _ShadowSkewY;
            float _ShadowAnchorMode;
            float4 _ShadowAnchorPivot;
            float _ShadowDepthBias;
            float _ShadowBlurStrength;

            StructuredBuffer<float4> translationAndRotationBuffer;
            StructuredBuffer<float2> scaleBuffer;
            StructuredBuffer<float4> uvBuffer;
            StructuredBuffer<int> frameIndexBuffer;
            StructuredBuffer<float2> flipBuffer;
            StructuredBuffer<float> renderDepthBuffer;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 frameMin : TEXCOORD1;
                float2 frameMax : TEXCOORD2;
            };

            fixed sampleShadowAlpha(float2 uv, float2 frameMin, float2 frameMax) {
                return tex2D(_MainTex, clamp(uv, frameMin, frameMax)).a;
            }

            fixed sampleBlurredShadowAlpha(float2 uv, float2 frameMin, float2 frameMax) {
                float blurRadius = max(_ShadowBlurStrength, 0.0);
                if (blurRadius <= 0.0001) {
                    return sampleShadowAlpha(uv, frameMin, frameMax);
                }

                float2 blurStep = _MainTex_TexelSize.xy * blurRadius;

                fixed center = sampleShadowAlpha(uv, frameMin, frameMax) * 4.0;
                fixed axial =
                    sampleShadowAlpha(uv + float2( blurStep.x, 0), frameMin, frameMax) +
                    sampleShadowAlpha(uv + float2(-blurStep.x, 0), frameMin, frameMax) +
                    sampleShadowAlpha(uv + float2(0,  blurStep.y), frameMin, frameMax) +
                    sampleShadowAlpha(uv + float2(0, -blurStep.y), frameMin, frameMax);
                fixed diagonal =
                    sampleShadowAlpha(uv + float2( blurStep.x,  blurStep.y), frameMin, frameMax) +
                    sampleShadowAlpha(uv + float2(-blurStep.x,  blurStep.y), frameMin, frameMax) +
                    sampleShadowAlpha(uv + float2( blurStep.x, -blurStep.y), frameMin, frameMax) +
                    sampleShadowAlpha(uv + float2(-blurStep.x, -blurStep.y), frameMin, frameMax);

                fixed blurred = (center + axial * 2.0 + diagonal) / 16.0;
                fixed blend = saturate(_ShadowBlurStrength / 4.0);
                return lerp(sampleShadowAlpha(uv, frameMin, frameMax), blurred, blend);
            }

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
                float2 unscaledLocalVertexXY = v.vertex.xy - float2(0.5, 0.5);
                float2 localVertexXY = unscaledLocalVertexXY * scale;

                if (_ShadowAnchorMode > 0.5) {
                    float2 anchorPivot = _ShadowAnchorPivot.xy;
                    float2 anchoredLocal = unscaledLocalVertexXY - anchorPivot;

                    anchoredLocal.x *= _ShadowScaleX;
                    anchoredLocal.y *= _ShadowScaleY;

                    float anchoredSkewedX = anchoredLocal.x + anchoredLocal.y * _ShadowSkewX;
                    float anchoredSkewedY = anchoredLocal.y + anchoredLocal.x * _ShadowSkewY;
                    anchoredLocal.x = anchoredSkewedX;
                    anchoredLocal.y = anchoredSkewedY;

                    float2 localOffset = float2(_ShadowLocalOffsetX, _ShadowLocalOffsetY);
                    float2 localShadowUnscaledXY = anchorPivot + anchoredLocal + localOffset;
                    localVertexXY = localShadowUnscaledXY * scale;
                } else {
                    localVertexXY.x *= _ShadowScaleX;
                    localVertexXY.y *= _ShadowScaleY;

                    float skewedX = localVertexXY.x + localVertexXY.y * _ShadowSkewX;
                    float skewedY = localVertexXY.y + localVertexXY.x * _ShadowSkewY;
                    localVertexXY.x = skewedX;
                    localVertexXY.y = skewedY;
                    localVertexXY.x += _ShadowLocalOffsetX;
                    localVertexXY.y += _ShadowLocalOffsetY;
                }

                float4 localVertex = float4(localVertexXY, 0, 0);

                float4 rotatedVertex = mul(localVertex, rotationZMatrix(translationAndRot.w));
                float3 worldPosition = translationAndRot.xyz + rotatedVertex.xyz;

                if (_ShadowAnchorMode <= 0.5) {
                    worldPosition.x += _ShadowOffsetX;
                    worldPosition.y += _ShadowOffsetY;
                }

                v2f o;
                o.pos = UnityObjectToClipPos(float4(worldPosition, 1.0f));
                float4 depthClipPos = UnityObjectToClipPos(float4(worldPosition.xy, renderDepth, 1.0f));
                o.pos.z = depthClipPos.z;

                o.uv = v.texcoord * uv.xy + uv.zw;
                o.frameMin = uv.zw;
                o.frameMax = uv.zw + uv.xy;

                float2 flip = flipBuffer[instanceID];
                float2 frameMin = o.frameMin;
                float2 frameMax = o.frameMax;
                if (flip.x > 0.5) o.uv.x = frameMax.x - (o.uv.x - frameMin.x);
                if (flip.y > 0.5) o.uv.y = frameMax.y - (o.uv.y - frameMin.y);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed sourceAlpha = sampleShadowAlpha(i.uv, i.frameMin, i.frameMax);
                fixed shadowAlpha = sampleBlurredShadowAlpha(i.uv, i.frameMin, i.frameMax);

                fixed alphaThreshold = lerp(_Cutoff, 0.001, saturate(_ShadowBlurStrength / 4.0));
                clip(shadowAlpha - alphaThreshold);

                fixed4 shadow = _ShadowColor;
                shadow.a *= max(sourceAlpha, shadowAlpha);
                return shadow;
            }
            ENDCG
        }
    }
}
