using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS2D.Rendering.Tests
{
    public sealed class SpriteAnimationClipTests
    {
        [Test]
        public void EvaluateFrameIndex_LoopsWithinClipLength()
        {
            var clip = new SpriteAnimationClipBlob
            {
                FrameCount = 4,
                FrameRate = 2f,
                Loop = 1,
                PingPong = 0
            };

            Assert.AreEqual(0, SpriteAnimationSetBlobUtility.EvaluateFrameIndex(in clip, 0f, 1f));
            Assert.AreEqual(1, SpriteAnimationSetBlobUtility.EvaluateFrameIndex(in clip, 0.5f, 1f));
            Assert.AreEqual(3, SpriteAnimationSetBlobUtility.EvaluateFrameIndex(in clip, 1.75f, 1f));
            Assert.AreEqual(0, SpriteAnimationSetBlobUtility.EvaluateFrameIndex(in clip, 2.0f, 1f));
        }

        [Test]
        public void ResolveSpriteFrameIndex_UsesRowAndStartColumn()
        {
            var set = new SpriteAnimationSetBlob
            {
                Columns = 6
            };

            var clip = new SpriteAnimationClipBlob
            {
                Row = 1,
                StartColumn = 1,
                FrameCount = 4
            };

            Assert.AreEqual(7, SpriteAnimationSetBlobUtility.ResolveSpriteFrameIndex(ref set, in clip, 0));
            Assert.AreEqual(8, SpriteAnimationSetBlobUtility.ResolveSpriteFrameIndex(ref set, in clip, 1));
            Assert.AreEqual(10, SpriteAnimationSetBlobUtility.ResolveSpriteFrameIndex(ref set, in clip, 3));
        }

        [Test]
        public void TryGetClipIndex_FindsNamedClip()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<SpriteAnimationSetBlob>();
            root.Columns = 6;

            var clips = builder.Allocate(ref root.Clips, 2);
            clips[0] = new SpriteAnimationClipBlob
            {
                Name = (FixedString64Bytes)"Idle",
                Row = 0,
                StartColumn = 0,
                FrameCount = 4
            };
            clips[1] = new SpriteAnimationClipBlob
            {
                Name = (FixedString64Bytes)"Run",
                Row = 1,
                StartColumn = 0,
                FrameCount = 6
            };

            using var blobRef = builder.CreateBlobAssetReference<SpriteAnimationSetBlob>(Allocator.Persistent);
            ref var set = ref blobRef.Value;

            Assert.IsTrue(SpriteAnimationSetBlobUtility.TryGetClipIndex(ref set, (FixedString64Bytes)"Run", out int clipIndex));
            Assert.AreEqual(1, clipIndex);
            Assert.IsFalse(SpriteAnimationSetBlobUtility.TryGetClipIndex(ref set, (FixedString64Bytes)"Missing", out _));
        }
    }
}
