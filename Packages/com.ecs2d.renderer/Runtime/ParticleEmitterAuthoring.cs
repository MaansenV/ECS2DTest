using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS2D.Rendering
{
    public class ParticleEmitterAuthoring : MonoBehaviour
    {
        public SpriteSheetDefinition SpriteSheet;
        public int SortingLayer;
        public int SpriteFrameIndex;
        public bool Enabled = true;
        public int BurstCount;
        public float SpawnRate;
        public int MaxParticles = 64;
        public float LifetimeMin = 1f;
        public float LifetimeMax = 1f;
        public float SpeedMin = 1f;
        public float SpeedMax = 1f;
        public float StartScale = 1f;
        public float EndScale = 0f;
        public Color StartColor = Color.white;
        public Color EndColor = Color.clear;
        public float CircleRadius = 0.5f;
        public ParticleCircleMode CircleMode = ParticleCircleMode.Area;
        public ParticleDirectionMode DirectionMode = ParticleDirectionMode.Outward;
        public Vector2 FixedDirection = Vector2.right;
        public float StartRotationMinDegrees;
        public float StartRotationMaxDegrees;
        public float RotationSpeedMinDegrees;
        public float RotationSpeedMaxDegrees;
        public float RestAfterSeconds = -1f;

        private sealed class Baker : Baker<ParticleEmitterAuthoring>
        {
            public override void Bake(ParticleEmitterAuthoring authoring)
            {
                if (authoring.SpriteSheet == null)
                {
                    Debug.LogError($"{nameof(ParticleEmitterAuthoring)} on '{authoring.name}' is missing a SpriteSheet reference.");
                    return;
                }

                DependsOn(authoring.transform);
                DependsOn(authoring.SpriteSheet);

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                int frameCount = math.max(1, authoring.SpriteSheet.FrameCount);
                int spriteFrameIndex = math.clamp(authoring.SpriteFrameIndex, 0, frameCount - 1);
                float lifetimeMin = math.min(authoring.LifetimeMin, authoring.LifetimeMax);
                float lifetimeMax = math.max(authoring.LifetimeMin, authoring.LifetimeMax);
                float speedMin = math.min(authoring.SpeedMin, authoring.SpeedMax);
                float speedMax = math.max(authoring.SpeedMin, authoring.SpeedMax);
                float rotationMin = math.radians(math.min(authoring.StartRotationMinDegrees, authoring.StartRotationMaxDegrees));
                float rotationMax = math.radians(math.max(authoring.StartRotationMinDegrees, authoring.StartRotationMaxDegrees));
                float rotationSpeedMin = math.radians(math.min(authoring.RotationSpeedMinDegrees, authoring.RotationSpeedMaxDegrees));
                float rotationSpeedMax = math.radians(math.max(authoring.RotationSpeedMinDegrees, authoring.RotationSpeedMaxDegrees));
                int maxParticles = math.max(1, authoring.MaxParticles);
                float restAfterSeconds = authoring.RestAfterSeconds < 0f ? -1f : math.max(0f, authoring.RestAfterSeconds);
                float spawnRate = authoring.Enabled ? math.max(0f, authoring.SpawnRate) : 0f;
                int burstCount = authoring.Enabled ? math.max(0, authoring.BurstCount) : 0;

                float4 startColor = new float4(authoring.StartColor.r, authoring.StartColor.g, authoring.StartColor.b, authoring.StartColor.a);
                float4 endColor = new float4(authoring.EndColor.r, authoring.EndColor.g, authoring.EndColor.b, authoring.EndColor.a);

                AddComponent(entity, new ParticleEmitter
                {
                    SheetId = authoring.SpriteSheet.SheetId,
                    SortingLayer = authoring.SortingLayer,
                    SpriteFrameIndex = spriteFrameIndex,
                    MaxParticles = maxParticles,
                    BurstCount = burstCount,
                    SpawnRate = spawnRate,
                    LifetimeMin = lifetimeMin,
                    LifetimeMax = lifetimeMax,
                    SpeedMin = speedMin,
                    SpeedMax = speedMax,
                    StartScale = math.max(0f, authoring.StartScale),
                    EndScale = math.max(0f, authoring.EndScale),
                    StartColor = startColor,
                    EndColor = endColor,
                    CircleRadius = math.max(0f, authoring.CircleRadius),
                    FixedDirection = authoring.FixedDirection,
                    StartRotationMinRadians = rotationMin,
                    StartRotationMaxRadians = rotationMax,
                    RotationSpeedMinRadians = rotationSpeedMin,
                    RotationSpeedMaxRadians = rotationSpeedMax,
                    RestAfterSeconds = restAfterSeconds,
                    CircleMode = (byte)authoring.CircleMode,
                    DirectionMode = (byte)authoring.DirectionMode,
                    EmitBurstOnStart = (byte)(burstCount > 0 ? 1 : 0)
                });
                AddComponent(entity, new ParticleEmitterRuntimeState
                {
                    SpawnAccumulator = 0f,
                    RestingExpiryAccumulator = 0f,
                    RandomState = 0u,
                    NextPoolIndex = 0,
                    BurstConsumed = 0
                });

                DynamicBuffer<ParticleEmitterParticleElement> buffer = AddBuffer<ParticleEmitterParticleElement>(entity);

                for (int i = 0; i < maxParticles; i++)
                {
                    Entity particleEntity = CreateAdditionalEntity(TransformUsageFlags.None, entityName: $"{authoring.name}-Particle-{i}");
                    buffer.Add(new ParticleEmitterParticleElement { Value = particleEntity });

                    AddComponent(particleEntity, new ParticleEmitterOwner
                    {
                        Value = entity
                    });
                    AddComponent(particleEntity, new ParticleRuntime
                    {
                        StartScale = math.max(0f, authoring.StartScale),
                        EndScale = math.max(0f, authoring.EndScale),
                        StartColor = startColor,
                        EndColor = endColor,
                        RestAfterSeconds = restAfterSeconds,
                        LifecycleState = (byte)ParticleLifecycleState.Inactive
                    });
                    AddComponent<ParticleActive>(particleEntity);
                    SetComponentEnabled<ParticleActive>(particleEntity, false);
                    AddComponent<ParticleResting>(particleEntity);
                    SetComponentEnabled<ParticleResting>(particleEntity, false);
                    AddComponent<SpriteCullState>(particleEntity);
                    SetComponentEnabled<SpriteCullState>(particleEntity, false);
                    AddComponent(particleEntity, new LocalToWorld
                    {
                        Value = float4x4.identity
                    });
                    AddComponent(particleEntity, new SpriteData
                    {
                        TranslationAndRotation = float4.zero,
                        BaseScale = 1f,
                        RotationOffsetRadians = 0f,
                        Scale = 0f,
                        Color = new float4(authoring.StartColor.r, authoring.StartColor.g, authoring.StartColor.b, 0f),
                        RenderDepth = 0f,
                        SpriteFrameIndex = spriteFrameIndex,
                        SpriteSheetId = authoring.SpriteSheet.SheetId,
                        SortingLayer = authoring.SortingLayer,
                        FlipX = 0,
                        FlipY = 0
                    });
                    AddSharedComponent(particleEntity, SpriteSheetRuntime.CreateRenderKey(authoring.SpriteSheet.SheetId));
                }
            }
        }
    }
}
