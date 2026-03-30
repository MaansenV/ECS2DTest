using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace ECS2D.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(SpriteAnimationSystem))]
    public partial struct SpriteAnimationChangeRequestSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var entityManager = state.EntityManager;
            using var entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (animationState, setRef, request, entity) in
                SystemAPI.Query<RefRW<SpriteAnimationState>, RefRO<SpriteAnimationSetReference>, RefRO<SpriteAnimationChangeRequest>>()
                    .WithEntityAccess())
            {
                BlobAssetReference<SpriteAnimationSetBlob> animationSetReference = setRef.ValueRO.Value;
                if (!animationSetReference.IsCreated || animationSetReference.Value.Clips.Length == 0)
                {
                    entityCommandBuffer.RemoveComponent<SpriteAnimationChangeRequest>(entity);
                    continue;
                }

                ref SpriteAnimationSetBlob animationSet = ref animationSetReference.Value;
                int clipIndex = request.ValueRO.ClipIndex;
                if (clipIndex < 0 || clipIndex >= animationSet.Clips.Length)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning(
                        $"Ignoring invalid {nameof(SpriteAnimationChangeRequest)} with clip index {clipIndex}. " +
                        $"Valid range is 0 to {animationSet.Clips.Length - 1}.");
#endif
                    entityCommandBuffer.RemoveComponent<SpriteAnimationChangeRequest>(entity);
                    continue;
                }

                ref SpriteAnimationState animation = ref animationState.ValueRW;
                animation.CurrentClipIndex = clipIndex;
                animation.CurrentFrameIndex = 0;
                animation.Time = request.ValueRO.Restart != 0 ? request.ValueRO.StartTime : 0f;
                animation.Flags |= SpriteAnimationState.InitializedFlag;

                entityCommandBuffer.RemoveComponent<SpriteAnimationChangeRequest>(entity);
            }

            entityCommandBuffer.Playback(entityManager);
        }
    }
}
