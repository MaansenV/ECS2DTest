using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;

namespace ECS2D.Rendering.Tests
{
    public sealed class SpriteAnimationSystemTests
    {
        [Test]
        public void Update_FallsBackToFirstClipAndCachesResolvedClip()
        {
            using var world = new World("SpriteAnimationSystemTests");
            var system = world.CreateSystem<SpriteAnimationSystem>();
            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity(typeof(SpriteAnimationState), typeof(SpriteAnimationSetReference), typeof(SpriteData));
            using var animationSet = CreateAnimationSet();

            entityManager.SetComponentData(entity, new SpriteAnimationSetReference
            {
                Value = animationSet
            });

            entityManager.SetComponentData(entity, new SpriteAnimationState
            {
                CurrentAnimation = (FixedString64Bytes)"Missing",
                LastResolvedAnimation = (FixedString64Bytes)"Run",
                Time = 1.25f,
                PlaybackSpeed = 1f,
                Playing = true,
                CurrentFrameIndex = 5,
                CurrentClipIndex = 99,
                Flags = SpriteAnimationState.InitializedFlag
            });

            entityManager.SetComponentData(entity, new SpriteData
            {
                SpriteFrameIndex = -1,
                SpriteSheetId = animationSet.Value.SpriteSheetId
            });

            world.SetTime(new TimeData(0.5, 0.5f));
            system.Update(world.Unmanaged);

            var state = entityManager.GetComponentData<SpriteAnimationState>(entity);
            var spriteData = entityManager.GetComponentData<SpriteData>(entity);

            Assert.AreEqual((FixedString64Bytes)"Idle", state.CurrentAnimation);
            Assert.AreEqual((FixedString64Bytes)"Idle", state.LastResolvedAnimation);
            Assert.AreEqual(0, state.CurrentClipIndex);
            Assert.AreEqual(1, state.CurrentFrameIndex);
            Assert.AreEqual(1, spriteData.SpriteFrameIndex);

            state.CurrentAnimation = (FixedString64Bytes)"Run";
            state.Time = 0f;
            state.CurrentFrameIndex = 0;
            entityManager.SetComponentData(entity, state);

            world.SetTime(new TimeData(1.0, 0.5f));
            system.Update(world.Unmanaged);

            state = entityManager.GetComponentData<SpriteAnimationState>(entity);
            spriteData = entityManager.GetComponentData<SpriteData>(entity);

            Assert.AreEqual((FixedString64Bytes)"Run", state.CurrentAnimation);
            Assert.AreEqual((FixedString64Bytes)"Run", state.LastResolvedAnimation);
            Assert.AreEqual(1, state.CurrentClipIndex);
            Assert.AreEqual(0, state.CurrentFrameIndex);
            Assert.AreEqual(5, spriteData.SpriteFrameIndex);

            world.SetTime(new TimeData(1.5, 0.5f));
            system.Update(world.Unmanaged);

            state = entityManager.GetComponentData<SpriteAnimationState>(entity);
            spriteData = entityManager.GetComponentData<SpriteData>(entity);

            Assert.AreEqual(1, state.CurrentClipIndex);
            Assert.AreEqual((FixedString64Bytes)"Run", state.LastResolvedAnimation);
            Assert.AreEqual(1, state.CurrentFrameIndex);
            Assert.AreEqual(6, spriteData.SpriteFrameIndex);
        }

        private static BlobAssetReference<SpriteAnimationSetBlob> CreateAnimationSet()
        {
            using var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<SpriteAnimationSetBlob>();
            root.SpriteSheetId = 7;
            root.Columns = 4;

            var clips = builder.Allocate(ref root.Clips, 2);
            clips[0] = new SpriteAnimationClipBlob
            {
                Name = (FixedString64Bytes)"Idle",
                Row = 0,
                StartColumn = 0,
                FrameCount = 2,
                FrameRate = 2f,
                Loop = 1
            };
            clips[1] = new SpriteAnimationClipBlob
            {
                Name = (FixedString64Bytes)"Run",
                Row = 1,
                StartColumn = 1,
                FrameCount = 3,
                FrameRate = 1f,
                Loop = 1
            };

            return builder.CreateBlobAssetReference<SpriteAnimationSetBlob>(Allocator.Persistent);
        }
    }
}
