using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;

namespace ECS2D.Rendering.Tests
{
    public sealed class SpriteAnimationChangeRequestSystemTests
    {
        [Test]
        public void SpriteAnimationChangeRequest_AppliesValidClipIndex()
        {
            using var world = new World("SpriteAnimationChangeRequestSystemTests");
            var system = world.CreateSystem<SpriteAnimationChangeRequestSystem>();
            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity(typeof(SpriteAnimationState), typeof(SpriteAnimationSetReference), typeof(SpriteAnimationChangeRequest));
            using var animationSet = CreateAnimationSet();

            entityManager.SetComponentData(entity, new SpriteAnimationSetReference
            {
                Value = animationSet
            });
            entityManager.SetComponentData(entity, new SpriteAnimationState
            {
                Time = 4f,
                PlaybackSpeed = 1f,
                CurrentClipIndex = 0,
                CurrentFrameIndex = 3,
                Flags = 0,
                Playing = 1
            });
            entityManager.SetComponentData(entity, new SpriteAnimationChangeRequest
            {
                ClipIndex = 1,
                StartTime = 1.25f,
                Restart = 1
            });

            system.Update(world.Unmanaged);

            var state = entityManager.GetComponentData<SpriteAnimationState>(entity);
            Assert.AreEqual(1, state.CurrentClipIndex);
            Assert.AreEqual(0, state.CurrentFrameIndex);
            Assert.AreEqual(1.25f, state.Time, 0.0001f);
            Assert.AreEqual(SpriteAnimationState.InitializedFlag, state.Flags & SpriteAnimationState.InitializedFlag);
            Assert.IsFalse(entityManager.HasComponent<SpriteAnimationChangeRequest>(entity));
        }

        [Test]
        public void SpriteAnimationChangeRequest_IgnoresInvalidClipIndex()
        {
            using var world = new World("SpriteAnimationChangeRequestSystemTests");
            var system = world.CreateSystem<SpriteAnimationChangeRequestSystem>();
            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity(typeof(SpriteAnimationState), typeof(SpriteAnimationSetReference), typeof(SpriteAnimationChangeRequest));
            using var animationSet = CreateAnimationSet();

            entityManager.SetComponentData(entity, new SpriteAnimationSetReference
            {
                Value = animationSet
            });
            entityManager.SetComponentData(entity, new SpriteAnimationState
            {
                Time = 2f,
                PlaybackSpeed = 1f,
                CurrentClipIndex = 0,
                CurrentFrameIndex = 1,
                Flags = SpriteAnimationState.InitializedFlag,
                Playing = 1
            });
            entityManager.SetComponentData(entity, new SpriteAnimationChangeRequest
            {
                ClipIndex = 99,
                StartTime = 5f,
                Restart = 1
            });

            system.Update(world.Unmanaged);

            var state = entityManager.GetComponentData<SpriteAnimationState>(entity);
            Assert.AreEqual(0, state.CurrentClipIndex);
            Assert.AreEqual(1, state.CurrentFrameIndex);
            Assert.AreEqual(2f, state.Time, 0.0001f);
            Assert.IsFalse(entityManager.HasComponent<SpriteAnimationChangeRequest>(entity));
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
                Name = "Idle",
                Row = 0,
                StartColumn = 0,
                FrameCount = 2,
                FrameRate = 2f,
                Loop = 1
            };
            clips[1] = new SpriteAnimationClipBlob
            {
                Name = "Run",
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
