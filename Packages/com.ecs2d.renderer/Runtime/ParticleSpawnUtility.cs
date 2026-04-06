using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

#if UNITY_EDITOR
using AnimationCurve = UnityEngine.AnimationCurve;
#endif

namespace ECS2D.Rendering
{
    public static class ParticleSpawnUtility
    {
        public static float SampleRange(ref Random random, float min, float max)
        {
            if (min > max)
            {
                (min, max) = (max, min);
            }

            if (math.abs(max - min) < 0.0001f)
            {
                return min;
            }

            return random.NextFloat(min, max);
        }

        public static float2 SampleCircleOffset(ref Random random, float radius, ParticleCircleMode mode)
        {
            radius = math.max(0f, radius);
            float angle = random.NextFloat(0f, math.PI * 2f);
            float2 direction = new float2(math.cos(angle), math.sin(angle));

            if (mode == ParticleCircleMode.Edge || radius <= 0f)
            {
                return direction * radius;
            }

            float sampledRadius = math.sqrt(random.NextFloat()) * radius;
            return direction * sampledRadius;
        }

        public static float2 ResolveDirection(
            ref Random random,
            ParticleDirectionMode mode,
            float2 spawnOffset,
            float2 fixedDirection)
        {
            switch (mode)
            {
                case ParticleDirectionMode.Fixed:
                    return NormalizeOrDefault(fixedDirection, new float2(1f, 0f));
                case ParticleDirectionMode.Random:
                {
                    float angle = random.NextFloat(0f, math.PI * 2f);
                    return new float2(math.cos(angle), math.sin(angle));
                }
                case ParticleDirectionMode.Outward:
                    return NormalizeOrDefault(spawnOffset, NormalizeOrDefault(fixedDirection, new float2(1f, 0f)));
                default:
                    return new float2(1f, 0f);
            }
        }

        public static float2 TransformDirection(in LocalToWorld localToWorld, float2 localDirection)
        {
            float2 xAxis = localToWorld.Value.c0.xy;
            float2 yAxis = localToWorld.Value.c1.xy;
            float2 worldDirection = (xAxis * localDirection.x) + (yAxis * localDirection.y);
            return NormalizeOrDefault(worldDirection, new float2(1f, 0f));
        }

        public static float3 TransformPoint(in LocalToWorld localToWorld, float2 localOffset)
        {
            float2 xAxis = localToWorld.Value.c0.xy;
            float2 yAxis = localToWorld.Value.c1.xy;
            float2 transformed = (xAxis * localOffset.x) + (yAxis * localOffset.y);
            return localToWorld.Position + new float3(transformed, 0f);
        }

        public static float EvaluateLifetimeFraction(float age, float lifetime)
        {
            if (lifetime <= 0.0001f)
            {
                return 1f;
            }

            return math.saturate(age / lifetime);
        }

        public static float EvaluateCurveLUT(BlobAssetReference<CurveBlobLUT> curve, float normalizedAge)
        {
            if (!curve.IsCreated)
            {
                return 1f;
            }

            int sampleCount = curve.Value.Samples.Length;
            if (sampleCount <= 0)
            {
                return 1f;
            }

            float t = math.saturate(normalizedAge);
            float exactIndex = t * (sampleCount - 1);
            int lower = (int)math.floor(exactIndex);
            int upper = math.min(lower + 1, sampleCount - 1);
            float frac = exactIndex - lower;
            return math.lerp(curve.Value.Samples[lower], curve.Value.Samples[upper], frac);
        }

#if UNITY_EDITOR
        public static BlobAssetReference<CurveBlobLUT> SampleAnimationCurveToBlob(AnimationCurve curve, int sampleCount)
        {
            sampleCount = math.max(1, sampleCount);

            using var builder = new BlobBuilder(Allocator.Temp);
            ref CurveBlobLUT root = ref builder.ConstructRoot<CurveBlobLUT>();
            BlobBuilderArray<float> samples = builder.Allocate(ref root.Samples, sampleCount);

            if (curve == null)
            {
                for (int i = 0; i < sampleCount; i++)
                {
                    samples[i] = 1f;
                }
            }
            else
            {
                float denominator = math.max(1, sampleCount - 1);
                for (int i = 0; i < sampleCount; i++)
                {
                    samples[i] = curve.Evaluate(i / denominator);
                }
            }

            return builder.CreateBlobAssetReference<CurveBlobLUT>(Allocator.Persistent);
        }
#endif

        public static BlobAssetReference<CurveBlobLUT> CreateFlatCurveBlob(int sampleCount, float value)
        {
            sampleCount = math.max(1, sampleCount);

            using var builder = new BlobBuilder(Allocator.Temp);
            ref CurveBlobLUT root = ref builder.ConstructRoot<CurveBlobLUT>();
            BlobBuilderArray<float> samples = builder.Allocate(ref root.Samples, sampleCount);

            for (int i = 0; i < sampleCount; i++)
            {
                samples[i] = value;
            }

            return builder.CreateBlobAssetReference<CurveBlobLUT>(Allocator.Persistent);
        }

        public static float4 EvaluateColor(float4 startColor, float4 endColor, float age, float lifetime)
            => math.lerp(startColor, endColor, EvaluateLifetimeFraction(age, lifetime));

        public static void WriteRenderState(
            ref ParticleRuntime runtime,
            ref SpriteData spriteData,
            ref LocalToWorld localToWorld)
        {
            spriteData.TranslationAndRotation = new float4(runtime.Position, runtime.RotationRadians);
            spriteData.Scale = runtime.BaseScale * EvaluateCurveLUT(runtime.ScaleCurve, EvaluateLifetimeFraction(runtime.Age, runtime.Lifetime));
            spriteData.Color = EvaluateColor(runtime.StartColor, runtime.EndColor, runtime.Age, runtime.Lifetime);
            spriteData.RenderDepth = SpriteSortingUtility.CalculateRenderDepth(
                spriteData.SortingLayer,
                runtime.Position.y,
                spriteData.SpriteSheetId,
                runtime.Position.z);

            localToWorld.Value = float4x4.TRS(
                runtime.Position,
                quaternion.RotateZ(runtime.RotationRadians),
                new float3(math.max(0.0001f, spriteData.Scale), math.max(0.0001f, spriteData.Scale), 1f));
        }

        public static float2 NormalizeOrDefault(float2 value, float2 fallback)
        {
            float lengthSq = math.lengthsq(value);
            if (lengthSq <= 0.000001f)
            {
                return fallback;
            }

            return value * math.rsqrt(lengthSq);
        }
    }
}
