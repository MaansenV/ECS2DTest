using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS2D.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(SpriteSystem))]
    public partial struct SpriteAnimationSystem : ISystem
    {
        [BurstCompile]
        private partial struct UpdateSpriteAnimationJob : IJobEntity
        {
            public float DeltaTime;

            private void Execute(ref SpriteAnimationState state, in SpriteAnimationSetReference setRef, ref SpriteData spriteData)
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
                    || state.CurrentClipIndex >= animationSet.Clips.Length
                    || !state.LastResolvedAnimation.Equals(state.CurrentAnimation);

                if (needsResolve)
                {
                    if (!SpriteAnimationSetBlobUtility.TryGetClipIndex(ref animationSet, in state.CurrentAnimation, out int clipIndex))
                    {
                        clipIndex = 0;
                        state.CurrentAnimation = animationSet.Clips[0].Name;
                        state.Time = 0f;
                        state.CurrentFrameIndex = 0;
                    }

                    state.CurrentClipIndex = clipIndex;
                    state.LastResolvedAnimation = state.CurrentAnimation;
                }

                ref readonly SpriteAnimationClipBlob clip = ref animationSet.Clips[state.CurrentClipIndex];
                float playbackSpeed = math.max(0f, state.PlaybackSpeed);

                if (state.Playing)
                {
                    state.Time += DeltaTime;
                }

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
