using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS2D.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateBefore(typeof(SpriteSystem))]
    public partial class SpriteAnimationSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (stateRW, setRefRO, spriteDataRW) in SystemAPI.Query<RefRW<SpriteAnimationState>, RefRO<SpriteAnimationSetReference>, RefRW<SpriteData>>())
            {
                ref SpriteAnimationState state = ref stateRW.ValueRW;
                BlobAssetReference<SpriteAnimationSetBlob> animationSetReference = setRefRO.ValueRO.Value;
                if (!animationSetReference.IsCreated)
                {
                    continue;
                }

                ref SpriteAnimationSetBlob animationSet = ref animationSetReference.Value;
                if (animationSet.Clips.Length == 0)
                {
                    continue;
                }

                int clipIndex = 0;
                bool clipFound = SpriteAnimationSetBlobUtility.TryGetClipIndex(ref animationSet, in state.CurrentAnimation, out clipIndex);

                if ((state.Flags & SpriteAnimationState.InitializedFlag) == 0)
                {
                    state.Flags |= SpriteAnimationState.InitializedFlag;
                    state.Time = 0f;
                    state.CurrentFrameIndex = 0;

                    if (!clipFound)
                    {
                        clipIndex = 0;
                        state.CurrentAnimation = animationSet.Clips[0].Name;
                    }
                }
                else if (!clipFound)
                {
                    clipIndex = 0;
                    state.Time = 0f;
                    state.CurrentFrameIndex = 0;
                    state.CurrentAnimation = animationSet.Clips[0].Name;
                }

                ref readonly SpriteAnimationClipBlob clip = ref animationSet.Clips[clipIndex];
                float playbackSpeed = math.max(0f, state.PlaybackSpeed);

                if (state.Playing)
                {
                    state.Time += deltaTime;
                }

                int clipFrameIndex = SpriteAnimationSetBlobUtility.EvaluateFrameIndex(in clip, state.Time, playbackSpeed);
                int spriteFrameIndex = SpriteAnimationSetBlobUtility.ResolveSpriteFrameIndex(ref animationSet, in clip, clipFrameIndex);

                state.CurrentFrameIndex = clipFrameIndex;
                spriteDataRW.ValueRW.SpriteSheetId = animationSet.SpriteSheetId;
                spriteDataRW.ValueRW.SpriteFrameIndex = spriteFrameIndex;
            }
        }
    }
}
