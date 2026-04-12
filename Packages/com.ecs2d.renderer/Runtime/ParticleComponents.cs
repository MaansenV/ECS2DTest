using Unity.Entities;
using Unity.Mathematics;

namespace ECS2D.Rendering
{
    public enum ParticleCircleMode : byte
    {
        Edge = 0,
        Area = 1
    }

    public enum ParticleDirectionMode : byte
    {
        Fixed = 0,
        Random = 1,
        Outward = 2
    }

    public enum ParticleLifecycleState : byte
    {
        Inactive = 0,
        Active = 1
    }

    public struct ParticleEmitter : IComponentData
    {
        public int SheetId;
        public int SortingLayer;
        public int SpriteFrameIndex;
        public int MaxParticles;
        public int BurstCount;
        public float SpawnRate;
        public float LifetimeMin;
        public float LifetimeMax;
        public float SpeedMin;
        public float SpeedMax;
        public BlobAssetReference<CurveBlobLUT> SpeedCurve;
        public BlobAssetReference<CurveBlobLUT> ScaleCurve;
        public float BaseScale;
        public float2 BaseScaleXY;
        public float4 StartColor;
        public float4 EndColor;
        public float CircleRadius;
        public float2 FixedDirection;
        public float StartRotationMinRadians;
        public float StartRotationMaxRadians;
        public float RotationSpeedMinRadians;
        public float RotationSpeedMaxRadians;
        public float DestroyEmitterAfterSeconds;
        public byte CircleMode;
        public byte DirectionMode;
        public byte SpeedCurveMode;
        public byte ScaleCurveMode;
        public byte EmitBurstOnStart;
    }

    public struct ParticleEmitterRuntimeState : IComponentData
    {
        public float SpawnAccumulator;
        public float EmitterAge;
        public int NextPoolIndex;
        public uint RandomState;
        public byte BurstConsumed;
    }

    [InternalBufferCapacity(16)]
    public struct ParticleEmitterParticleElement : IBufferElementData
    {
        public Entity Value;
    }

    public struct ParticleEmitterOwner : IComponentData
    {
        public Entity Value;
    }

    public struct ParticleRuntime : IComponentData
    {
        public float3 Position;
        public float2 Velocity;
        public float Age;
        public float Lifetime;
        public BlobAssetReference<CurveBlobLUT> SpeedCurve;
        public BlobAssetReference<CurveBlobLUT> ScaleCurve;
        public float RotationRadians;
        public float RotationSpeedRadians;
        public float InitialSpeed;
        public float CurrentSpeed;
        public float BaseScale;
        public float2 BaseScaleXY;
        public float4 StartColor;
        public float4 EndColor;
        public byte LifecycleState;
    }

    public struct ParticleActive : IComponentData, IEnableableComponent
    {
    }
}
