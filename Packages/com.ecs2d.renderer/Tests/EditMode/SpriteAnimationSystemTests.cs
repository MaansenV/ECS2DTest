using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;

namespace ECS2D.Rendering.Tests
{
    public sealed class SpriteAnimationSystemTests
    {
        [Test]
        public void Update_FallsBackToFirstClipWhenClipIndexIsInvalid()
        {
            using var world = new World("SpriteAnimationSystemTests");
            var system = world.CreateSystem<SpriteAnimationSystem>();
            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity(typeof(SpriteAnimationState), typeof(SpriteAnimationSetReference), typeof(SpriteData), typeof(SpriteCullState));
            using var animationSet = CreateAnimationSet();

            entityManager.SetComponentData(entity, new SpriteAnimationSetReference
            {
                Value = animationSet
            });

            entityManager.SetComponentData(entity, new SpriteAnimationState
            {
                Time = 1.25f,
                PlaybackSpeed = 1f,
                CurrentClipIndex = 99,
                CurrentFrameIndex = 5,
                Flags = SpriteAnimationState.InitializedFlag,
                Playing = 1
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

            Assert.AreEqual(0, state.CurrentClipIndex);
            Assert.AreEqual(0.5f, state.Time, 0.0001f);
            Assert.AreEqual(1, state.CurrentFrameIndex);
            Assert.AreEqual(1, spriteData.SpriteFrameIndex);
        }

        [Test]
        public void Update_UsesCurrentClipIndexWithoutNameResolution()
        {
            using var world = new World("SpriteAnimationSystemTests");
            var system = world.CreateSystem<SpriteAnimationSystem>();
            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity(typeof(SpriteAnimationState), typeof(SpriteAnimationSetReference), typeof(SpriteData), typeof(SpriteCullState));
            using var animationSet = CreateAnimationSet();

            entityManager.SetComponentData(entity, new SpriteAnimationSetReference
            {
                Value = animationSet
            });

            entityManager.SetComponentData(entity, new SpriteAnimationState
            {
                Time = 0f,
                PlaybackSpeed = 1f,
                CurrentClipIndex = 1,
                CurrentFrameIndex = 0,
                Flags = SpriteAnimationState.InitializedFlag,
                Playing = 1
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

            Assert.AreEqual(1, state.CurrentClipIndex);
            Assert.AreEqual(0.5f, state.Time, 0.0001f);
            Assert.AreEqual(0, state.CurrentFrameIndex);
            Assert.AreEqual(5, spriteData.SpriteFrameIndex);

            world.SetTime(new TimeData(1.0, 0.5f));
            system.Update(world.Unmanaged);

            state = entityManager.GetComponentData<SpriteAnimationState>(entity);
            spriteData = entityManager.GetComponentData<SpriteData>(entity);

            Assert.AreEqual(1, state.CurrentClipIndex);
            Assert.AreEqual(1, state.CurrentFrameIndex);
            Assert.AreEqual(6, spriteData.SpriteFrameIndex);

            world.SetTime(new TimeData(1.5, 0.5f));
            system.Update(world.Unmanaged);

            state = entityManager.GetComponentData<SpriteAnimationState>(entity);
            spriteData = entityManager.GetComponentData<SpriteData>(entity);

            Assert.AreEqual(1, state.CurrentClipIndex);
            Assert.AreEqual(1, state.CurrentFrameIndex);
            Assert.AreEqual(6, spriteData.SpriteFrameIndex);
        }

        [Test]
        public void Update_AdvancesManyEntitiesWithCompactState()
        {
            using var world = new World("SpriteAnimationSystemTests");
            var system = world.CreateSystem<SpriteAnimationSystem>();
            var entityManager = world.EntityManager;
            using var animationSet = CreateAnimationSet();

            const int entityCount = 2048;
            EntityArchetype archetype = entityManager.CreateArchetype(typeof(SpriteAnimationState), typeof(SpriteAnimationSetReference), typeof(SpriteData), typeof(SpriteCullState));

            for (int i = 0; i < entityCount; i++)
            {
                Entity entity = entityManager.CreateEntity(archetype);
                entityManager.SetComponentData(entity, new SpriteAnimationSetReference
                {
                    Value = animationSet
                });
                entityManager.SetComponentData(entity, new SpriteAnimationState
                {
                    Time = 0f,
                    PlaybackSpeed = 1f,
                    CurrentClipIndex = i % 2,
                    CurrentFrameIndex = 0,
                    Flags = SpriteAnimationState.InitializedFlag,
                    Playing = 1
                });
                entityManager.SetComponentData(entity, new SpriteData
                {
                    SpriteFrameIndex = -1,
                    SpriteSheetId = animationSet.Value.SpriteSheetId
                });
            }

            world.SetTime(new TimeData(0.5, 0.5f));
            system.Update(world.Unmanaged);

            var query = entityManager.CreateEntityQuery(typeof(SpriteAnimationState), typeof(SpriteData));
            using var states = query.ToComponentDataArray<SpriteAnimationState>(Allocator.Temp);
            using var spriteDataArray = query.ToComponentDataArray<SpriteData>(Allocator.Temp);

            Assert.AreEqual(entityCount, states.Length);
            for (int i = 0; i < states.Length; i++)
            {
                Assert.GreaterOrEqual(states[i].CurrentClipIndex, 0);
                Assert.Less(states[i].CurrentClipIndex, 2);
                Assert.GreaterOrEqual(spriteDataArray[i].SpriteFrameIndex, 0);
            }
        }

        [Test]
        public void Update_SkipsDisabledCullEntities()
        {
            using var world = new World("SpriteAnimationCulledSkipTests");
            var system = world.CreateSystem<SpriteAnimationSystem>();
            var entityManager = world.EntityManager;
            using var animationSet = CreateAnimationSet();

            var entity = entityManager.CreateEntity(typeof(SpriteAnimationState), typeof(SpriteAnimationSetReference), typeof(SpriteData), typeof(SpriteCullState));
            entityManager.SetComponentData(entity, new SpriteAnimationSetReference
            {
                Value = animationSet
            });
            entityManager.SetComponentData(entity, new SpriteAnimationState
            {
                Time = 0f,
                PlaybackSpeed = 1f,
                CurrentClipIndex = 0,
                CurrentFrameIndex = 0,
                Flags = SpriteAnimationState.InitializedFlag,
                Playing = 1
            });
            entityManager.SetComponentData(entity, new SpriteData
            {
                SpriteFrameIndex = -1,
                SpriteSheetId = animationSet.Value.SpriteSheetId
            });
            entityManager.SetComponentEnabled<SpriteCullState>(entity, false);

            world.SetTime(new TimeData(0.5, 0.5f));
            system.Update(world.Unmanaged);

            var state = entityManager.GetComponentData<SpriteAnimationState>(entity);
            var spriteData = entityManager.GetComponentData<SpriteData>(entity);

            Assert.AreEqual(0f, state.Time, 0.0001f);
            Assert.AreEqual(-1, spriteData.SpriteFrameIndex);
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
