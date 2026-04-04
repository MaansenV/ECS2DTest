using NUnit.Framework;
using Unity.Mathematics;
using Unity.Transforms;

namespace ECS2D.Rendering.Tests
{
    public sealed class ParticleSpawnUtilityTests
    {
        [Test]
        public void SampleCircleOffset_EdgeMode_StaysOnRadius()
        {
            var random = new Random(1234);

            for (int i = 0; i < 32; i++)
            {
                float2 offset = ParticleSpawnUtility.SampleCircleOffset(ref random, 2f, ParticleCircleMode.Edge);
                Assert.That(math.length(offset), Is.EqualTo(2f).Within(0.0001f));
            }
        }

        [Test]
        public void SampleCircleOffset_AreaMode_StaysInsideRadius()
        {
            var random = new Random(5678);

            for (int i = 0; i < 64; i++)
            {
                float2 offset = ParticleSpawnUtility.SampleCircleOffset(ref random, 3f, ParticleCircleMode.Area);
                Assert.That(math.length(offset), Is.LessThanOrEqualTo(3f + 0.0001f));
            }
        }

        [Test]
        public void ResolveDirection_Outward_UsesSpawnOffsetDirection()
        {
            var random = new Random(999);
            float2 direction = ParticleSpawnUtility.ResolveDirection(
                ref random,
                ParticleDirectionMode.Outward,
                new float2(2f, 0f),
                new float2(0f, 1f));

            Assert.That(direction.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(direction.y, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void TransformDirection_RotatesLocalVectorByEmitterTransform()
        {
            var localToWorld = new LocalToWorld
            {
                Value = float4x4.TRS(
                    new float3(0f, 0f, 0f),
                    quaternion.RotateZ(math.radians(90f)),
                    new float3(1f, 1f, 1f))
            };

            float2 worldDirection = ParticleSpawnUtility.TransformDirection(localToWorld, new float2(1f, 0f));

            Assert.That(worldDirection.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(worldDirection.y, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void EvaluateSpeedMultiplier_DropsToZeroAtRestTime()
        {
            Assert.That(ParticleSpawnUtility.EvaluateSpeedMultiplier(0f, 2f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(ParticleSpawnUtility.EvaluateSpeedMultiplier(1f, 2f), Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(ParticleSpawnUtility.EvaluateSpeedMultiplier(2f, 2f), Is.EqualTo(0f).Within(0.0001f));
        }
    }
}
