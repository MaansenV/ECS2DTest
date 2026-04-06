using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace ECS2D.Rendering
{
    public class ParticleEmitterAuthoring : MonoBehaviour
    {
        [Tooltip("Sprite sheet that supplies the particle frames and render key.")]
        public SpriteSheetDefinition SpriteSheet;
        [Tooltip("Sorting layer used when particles are converted into sprite data.")]
        public int SortingLayer;
        [Tooltip("Initial sprite frame index used for emitted particles.")]
        public int SpriteFrameIndex;
        [Tooltip("Enables or disables emission from this authoring component.")]
        public bool Enabled = true;
        [Tooltip("Number of particles emitted in the initial burst.")]
        public int BurstCount;
        [Tooltip("Continuous spawn rate in particles per second.")]
        [Range(0f, 100f)]
        public float SpawnRate;
        [Tooltip("Maximum number of pooled particles created for this emitter.")]
        [Range(1, 10000)]
        public int MaxParticles = 64;
        [Tooltip("Minimum particle lifetime in seconds.")]
        [Range(0.01f, 60f)]
        public float LifetimeMin = 1f;
        [Tooltip("Maximum particle lifetime in seconds.")]
        [Range(0.01f, 60f)]
        public float LifetimeMax = 1f;
        [Tooltip("Minimum particle speed.")]
        [Range(0f, 100f)]
        public float SpeedMin = 1f;
        [Tooltip("Maximum particle speed.")]
        [Range(0f, 100f)]
        public float SpeedMax = 1f;
        [Tooltip("Curve that shapes particle speed over lifetime.")]
        public AnimationCurve SpeedCurve = AnimationCurve.Constant(0f, 1f, 1f);
        [Tooltip("Curve that shapes particle scale over lifetime.")]
        public AnimationCurve ScaleCurve = AnimationCurve.Constant(0f, 1f, 1f);
        [Tooltip("How the speed curve is interpreted during particle lifetime.")]
        public ParticleCurveMode SpeedCurveMode = ParticleCurveMode.Constant;
        [Tooltip("How the scale curve is interpreted during particle lifetime.")]
        public ParticleCurveMode ScaleCurveMode = ParticleCurveMode.Constant;
        [Tooltip("Base particle scale applied before curve evaluation.")]
        [Range(0.01f, 100f)]
        public float BaseScale = 1f;
        [Tooltip("Particle start color.")]
        public Color StartColor = Color.white;
        [Tooltip("Particle end color.")]
        public Color EndColor = Color.clear;
        [Tooltip("Radius used when emitting particles from a circle shape.")]
        [Range(0f, 50f)]
        public float CircleRadius = 0.5f;
        [Tooltip("Controls whether particles spawn on the circle edge or across its area.")]
        public ParticleCircleMode CircleMode = ParticleCircleMode.Area;
        [Tooltip("Controls how particle directions are chosen at spawn.")]
        public ParticleDirectionMode DirectionMode = ParticleDirectionMode.Outward;
        [Tooltip("Fixed emission direction used when direction mode is set to fixed.")]
        public Vector2 FixedDirection = Vector2.right;
        [Tooltip("Minimum initial rotation in degrees for emitted particles.")]
        [Range(-360f, 360f)]
        public float StartRotationMinDegrees;
        [Tooltip("Maximum initial rotation in degrees for emitted particles.")]
        [Range(-360f, 360f)]
        public float StartRotationMaxDegrees;
        [Tooltip("Minimum angular velocity in degrees per second.")]
        [Range(-720f, 720f)]
        public float RotationSpeedMinDegrees;
        [Tooltip("Maximum angular velocity in degrees per second.")]
        [Range(-720f, 720f)]
        public float RotationSpeedMaxDegrees;
        [Tooltip("Time in seconds before the emitter destroys itself. Set below zero to disable auto-destruction.")]
        public float DestroyEmitterAfterSeconds = -1f;

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
                float destroyEmitterAfterSeconds = authoring.DestroyEmitterAfterSeconds < 0f ? -1f : math.max(0f, authoring.DestroyEmitterAfterSeconds);
                float spawnRate = authoring.Enabled ? math.max(0f, authoring.SpawnRate) : 0f;
                int burstCount = authoring.Enabled ? math.max(0, authoring.BurstCount) : 0;
                BlobAssetReference<CurveBlobLUT> speedCurve = ParticleSpawnUtility.SampleAnimationCurveToBlob(authoring.SpeedCurve, CurveBlobLUT.kSampleCount);
                BlobAssetReference<CurveBlobLUT> scaleCurve = ParticleSpawnUtility.SampleAnimationCurveToBlob(authoring.ScaleCurve, CurveBlobLUT.kSampleCount);

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
                    SpeedCurve = speedCurve,
                    ScaleCurve = scaleCurve,
                    SpeedCurveMode = (byte)authoring.SpeedCurveMode,
                    ScaleCurveMode = (byte)authoring.ScaleCurveMode,
                    BaseScale = math.max(0f, authoring.BaseScale),
                    StartColor = startColor,
                    EndColor = endColor,
                    CircleRadius = math.max(0f, authoring.CircleRadius),
                    FixedDirection = authoring.FixedDirection,
                    StartRotationMinRadians = rotationMin,
                    StartRotationMaxRadians = rotationMax,
                    RotationSpeedMinRadians = rotationSpeedMin,
                    RotationSpeedMaxRadians = rotationSpeedMax,
                    DestroyEmitterAfterSeconds = destroyEmitterAfterSeconds,
                    CircleMode = (byte)authoring.CircleMode,
                    DirectionMode = (byte)authoring.DirectionMode,
                    EmitBurstOnStart = (byte)(burstCount > 0 ? 1 : 0)
                });
                AddComponent(entity, new ParticleEmitterRuntimeState
                {
                    SpawnAccumulator = 0f,
                    RandomState = 0u,
                    NextPoolIndex = 0,
                    BurstConsumed = 0
                });

                DynamicBuffer<ParticleEmitterParticleElement> buffer = AddBuffer<ParticleEmitterParticleElement>(entity);

                for (int i = 0; i < maxParticles; i++)
                {
                    Entity particleEntity = CreateAdditionalEntity(TransformUsageFlags.ManualOverride, entityName: $"{authoring.name}-Particle-{i}");
                    buffer.Add(new ParticleEmitterParticleElement { Value = particleEntity });

                    AddComponent(particleEntity, new ParticleEmitterOwner
                    {
                        Value = entity
                    });
                    AddComponent(particleEntity, new ParticleRuntime
                    {
                        SpeedCurve = speedCurve,
                        ScaleCurve = scaleCurve,
                        BaseScale = math.max(0f, authoring.BaseScale),
                        StartColor = startColor,
                        EndColor = endColor,
                        LifecycleState = (byte)ParticleLifecycleState.Inactive
                    });
                    AddComponent<ParticleActive>(particleEntity);
                    SetComponentEnabled<ParticleActive>(particleEntity, false);
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
