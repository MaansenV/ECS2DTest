using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using ECS2D.Rendering;

#if UNITY_EDITOR
using UnityEngine;
#endif

namespace ECS2D.Rendering.Tests
{
    public sealed class ParticleCurveLUTTests
    {
        [Test]
        public void EvaluateCurveLUT_FlatCurve_ReturnsConstantValue()
        {
            BlobAssetReference<CurveBlobLUT> blob = ParticleSpawnUtility.CreateFlatCurveBlob(CurveBlobLUT.kSampleCount, 1f);

            try
            {
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 0f), Is.EqualTo(1f).Within(0.001f));
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 0.25f), Is.EqualTo(1f).Within(0.001f));
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 0.5f), Is.EqualTo(1f).Within(0.001f));
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 0.75f), Is.EqualTo(1f).Within(0.001f));
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 1f), Is.EqualTo(1f).Within(0.001f));
            }
            finally
            {
                if (blob.IsCreated)
                {
                    blob.Dispose();
                }
            }
        }

        [Test]
        public void EvaluateCurveLUT_LinearRamp_EvaluatesCorrectly()
        {
            BlobAssetReference<CurveBlobLUT> blob = CreateLinearRampBlob();

            try
            {
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 0f), Is.EqualTo(0f).Within(0.02f));
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 0.5f), Is.EqualTo(0.5f).Within(0.02f));
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 1f), Is.EqualTo(1f).Within(0.02f));
            }
            finally
            {
                if (blob.IsCreated)
                {
                    blob.Dispose();
                }
            }
        }

        [Test]
        public void EvaluateCurveLUT_ClampsOutOfRange()
        {
            BlobAssetReference<CurveBlobLUT> blob = CreateLinearRampBlob();

            try
            {
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, -0.5f), Is.EqualTo(0f).Within(0.02f));
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 1.5f), Is.EqualTo(1f).Within(0.02f));
            }
            finally
            {
                if (blob.IsCreated)
                {
                    blob.Dispose();
                }
            }
        }

        [Test]
        public void EvaluateCurveLUT_UncreatedBlob_ReturnsOne()
        {
            BlobAssetReference<CurveBlobLUT> blob = default;

            Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 0.5f), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void EvaluateCurveLUT_SingleSampleBlob_ReturnsThatValue()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref CurveBlobLUT root = ref builder.ConstructRoot<CurveBlobLUT>();
            BlobBuilderArray<float> samples = builder.Allocate(ref root.Samples, 1);
            samples[0] = 0.75f;

            BlobAssetReference<CurveBlobLUT> blob = builder.CreateBlobAssetReference<CurveBlobLUT>(Allocator.Persistent);

            try
            {
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 0f), Is.EqualTo(0.75f).Within(0.001f));
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 0.5f), Is.EqualTo(0.75f).Within(0.001f));
                Assert.That(ParticleSpawnUtility.EvaluateCurveLUT(blob, 1f), Is.EqualTo(0.75f).Within(0.001f));
            }
            finally
            {
                if (blob.IsCreated)
                {
                    blob.Dispose();
                }
            }
        }

        [Test]
        public void CreateFlatCurveBlob_AllValuesMatch()
        {
            BlobAssetReference<CurveBlobLUT> blob = ParticleSpawnUtility.CreateFlatCurveBlob(CurveBlobLUT.kSampleCount, 0.5f);

            try
            {
                Assert.AreEqual(CurveBlobLUT.kSampleCount, blob.Value.Samples.Length);

                for (int i = 0; i < blob.Value.Samples.Length; i++)
                {
                    Assert.That(blob.Value.Samples[i], Is.EqualTo(0.5f).Within(0.001f));
                }
            }
            finally
            {
                if (blob.IsCreated)
                {
                    blob.Dispose();
                }
            }
        }

#if UNITY_EDITOR
        [Test]
        public void SampleAnimationCurveToBlob_LinearCurve_SamplesCorrectly()
        {
            AnimationCurve curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));
            BlobAssetReference<CurveBlobLUT> blob = ParticleSpawnUtility.SampleAnimationCurveToBlob(curve, CurveBlobLUT.kSampleCount);

            try
            {
                Assert.AreEqual(CurveBlobLUT.kSampleCount, blob.Value.Samples.Length);
                Assert.That(blob.Value.Samples[0], Is.EqualTo(0f).Within(0.02f));
                Assert.That(blob.Value.Samples[blob.Value.Samples.Length - 1], Is.EqualTo(1f).Within(0.02f));
            }
            finally
            {
                if (blob.IsCreated)
                {
                    blob.Dispose();
                }
            }
        }

        [Test]
        public void SampleAnimationCurveToBlob_NullCurve_ReturnsFlatOnes()
        {
            BlobAssetReference<CurveBlobLUT> blob = ParticleSpawnUtility.SampleAnimationCurveToBlob(null, CurveBlobLUT.kSampleCount);

            try
            {
                Assert.AreEqual(CurveBlobLUT.kSampleCount, blob.Value.Samples.Length);

                for (int i = 0; i < blob.Value.Samples.Length; i++)
                {
                    Assert.That(blob.Value.Samples[i], Is.EqualTo(1f).Within(0.001f));
                }
            }
            finally
            {
                if (blob.IsCreated)
                {
                    blob.Dispose();
                }
            }
        }
#endif

        private static BlobAssetReference<CurveBlobLUT> CreateLinearRampBlob()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref CurveBlobLUT root = ref builder.ConstructRoot<CurveBlobLUT>();
            BlobBuilderArray<float> samples = builder.Allocate(ref root.Samples, CurveBlobLUT.kSampleCount);

            for (int i = 0; i < CurveBlobLUT.kSampleCount; i++)
            {
                samples[i] = i / 63f;
            }

            return builder.CreateBlobAssetReference<CurveBlobLUT>(Allocator.Persistent);
        }
    }
}
