using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS2D.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(SpriteCullingSystem))]
    [UpdateBefore(typeof(SpriteSystem))]
    public partial struct SpriteAnimationSystem : ISystem
    {
        [BurstCompile]
        private partial struct UpdateSpriteAnimationJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(ref SpriteAnimationState state, in SpriteAnimationSetReference setRef, ref SpriteData spriteData, in SpriteCullState cullState)
            {
                BlobAssetReference<SpriteAnimationSetBlob> animationSetReference = setRef.Value;
                if (!animationSetReference.IsCreated)
                {
                    return;
                }

                ref SpriteAnimationSetBlob animationSet = ref animationSetReference.Value;
                if (animationSet.Clips.Length == 0)
                {
                    return;
                }

                bool wasInitialized = (state.Flags & SpriteAnimationState.InitializedFlag) != 0;
                if (!wasInitialized)
                {
                    state.Flags |= SpriteAnimationState.InitializedFlag;
                    state.Time = 0f;
                    state.CurrentFrameIndex = 0;
                }

                bool needsResolve = !wasInitialized
                    || state.CurrentClipIndex < 0
                    || state.CurrentClipIndex >= animationSet.Clips.Length;

                if (needsResolve)
                {
                    state.CurrentClipIndex = 0;
                    state.Time = 0f;
                    state.CurrentFrameIndex = 0;
                }

                ref readonly SpriteAnimationClipBlob clip = ref animationSet.Clips[state.CurrentClipIndex];
                float playbackSpeed = math.max(0f, state.PlaybackSpeed);

                state.Time += DeltaTime * state.Playing;

                int clipFrameIndex = SpriteAnimationSetBlobUtility.EvaluateFrameIndex(in clip, state.Time, playbackSpeed);
                int spriteFrameIndex = SpriteAnimationSetBlobUtility.ResolveSpriteFrameIndex(ref animationSet, in clip, clipFrameIndex);

                state.CurrentFrameIndex = clipFrameIndex;
                spriteData.SpriteFrameIndex = spriteFrameIndex;
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new UpdateSpriteAnimationJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel(state.Dependency);
        }
    }
}
