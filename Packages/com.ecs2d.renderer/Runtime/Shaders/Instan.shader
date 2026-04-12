Shader "Instanced/SpriteRendererIndexedUv" {
    Properties {
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
    }

    SubShader {
        Tags {
            "Queue"="AlphaTest"
            "IgnoreProjector"="True"
            "RenderType"="TransparentCutout"
        }
        Cull Back
        Lighting Off
        ZWrite On
        AlphaTest Greater 0
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

            StructuredBuffer<float4> translationAndRotationBuffer;
            StructuredBuffer<float2> scaleBuffer;
            StructuredBuffer<float4> colorsBuffer;
            StructuredBuffer<float4> uvBuffer;
            StructuredBuffer<int> frameIndexBuffer;
            StructuredBuffer<float2> flipBuffer;
            StructuredBuffer<float> renderDepthBuffer;

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR0;
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
                float renderDepth = renderDepthBuffer[instanceID];
                int frameIndex = frameIndexBuffer[instanceID];
                float4 uv = uvBuffer[frameIndex];

                // Scale it
                float2 scale = scaleBuffer[instanceID];
                float4 localVertex = v.vertex - float4(0.5, 0.5, 0, 0);
                localVertex.x *= scale.x;
                localVertex.y *= scale.y;

                // Rotate the scaled vertex
                float4 rotatedVertex = mul(localVertex, rotationZMatrix(translationAndRot.w));
                float3 worldPosition = translationAndRot.xyz + rotatedVertex.xyz;

                v2f o;
                o.pos = UnityObjectToClipPos(float4(worldPosition, 1.0f));
                float4 depthClipPos = UnityObjectToClipPos(float4(worldPosition.xy, renderDepth, 1.0f));
                o.pos.z = depthClipPos.z;

                // Apply UV transformation
                o.uv = v.texcoord * uv.xy + uv.zw;

                // Apply flip
                float2 flip = flipBuffer[instanceID];
                float2 frameMin = uv.zw;
                float2 frameMax = uv.zw + uv.xy;
                if (flip.x > 0.5) o.uv.x = frameMax.x - (o.uv.x - frameMin.x);
                if (flip.y > 0.5) o.uv.y = frameMax.y - (o.uv.y - frameMin.y);

                o.color = colorsBuffer[instanceID];
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv) * i.color;
                clip(col.a - _Cutoff);
                col.rgb *= col.a;

                return col;
            }
            ENDCG
        }
    }
}
