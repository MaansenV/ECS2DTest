using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace ECS2D.Rendering
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class SpriteSystem : SystemBase
    {
        private EntityQuery allSpriteQuery;
        private EntityQuery filteredSpriteQuery;
        private readonly Dictionary<int, SpriteRenderGroup> renderGroups = new Dictionary<int, SpriteRenderGroup>();
        private Mesh quadMesh;
        private bool hasLoggedMissingDefinitions;
        private bool hasLoggedUnmatchedSprites;
        private int lastDefinitionsSignature = int.MinValue;

        private static readonly int TranslationAndRotationBufferId = Shader.PropertyToID("translationAndRotationBuffer");
        private static readonly int ScaleBufferId = Shader.PropertyToID("scaleBuffer");
        private static readonly int ColorsBufferId = Shader.PropertyToID("colorsBuffer");
        private static readonly int UvBufferId = Shader.PropertyToID("uvBuffer");
        private static readonly int FrameIndexBufferId = Shader.PropertyToID("frameIndexBuffer");

        [BurstCompile]
        private struct UploadSpriteDataJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<SpriteData> SpriteDataType;
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<int> ChunkBaseEntityIndices;
            [NativeDisableParallelForRestriction] public NativeArray<float4> TranslationAndRotationOutput;
            [NativeDisableParallelForRestriction] public NativeArray<float> ScaleOutput;
            [NativeDisableParallelForRestriction] public NativeArray<float4> ColorOutput;
            [NativeDisableParallelForRestriction] public NativeArray<int> FrameIndexOutput;
            public int MaxFrameIndex;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var spriteDataArray = chunk.GetNativeArray(ref SpriteDataType);
                int baseEntityIndex = ChunkBaseEntityIndices[unfilteredChunkIndex];

                if (!useEnabledMask)
                {
                    for (int i = 0; i < spriteDataArray.Length; i++)
                    {
                        WriteSprite(spriteDataArray[i], baseEntityIndex + i);
                    }

                    return;
                }

                int enabledEntityIndex = 0;
                var enumerator = new ChunkEntityEnumerator(useEnabledMask, chunkEnabledMask, chunk.Count);
                while (enumerator.NextEntityIndex(out int i))
                {
                    WriteSprite(spriteDataArray[i], baseEntityIndex + enabledEntityIndex);
                    enabledEntityIndex++;
                }
            }

            private void WriteSprite(in SpriteData spriteData, int outputIndex)
            {
                TranslationAndRotationOutput[outputIndex] = spriteData.TranslationAndRotation;
                ScaleOutput[outputIndex] = spriteData.Scale;
                ColorOutput[outputIndex] = spriteData.Color;
                FrameIndexOutput[outputIndex] = math.clamp(spriteData.SpriteFrameIndex, 0, MaxFrameIndex);
            }
        }

        protected override void OnCreate()
        {
            allSpriteQuery = GetEntityQuery(ComponentType.ReadOnly<SpriteData>());
            filteredSpriteQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpriteData>(),
                ComponentType.ReadOnly<SpriteSheetRenderKey>());
        }

        protected override void OnStartRunning()
        {
            BuildRenderGroups();
        }

        protected override void OnUpdate()
        {
            int currentDefinitionsSignature = SpriteSheetDatabase.GetDefinitionsSignature();
            if (currentDefinitionsSignature != lastDefinitionsSignature)
            {
                BuildRenderGroups();
            }

            if (renderGroups.Count == 0)
            {
                if (!hasLoggedMissingDefinitions)
                {
                    Debug.LogWarning("SpriteSystem did not find any SpriteSheetDefinition assets in Resources/SpriteSheets.");
                    hasLoggedMissingDefinitions = true;
                }

                return;
            }

            int totalSpriteCount = allSpriteQuery.CalculateEntityCount();
            if (totalSpriteCount == 0)
            {
                return;
            }

            filteredSpriteQuery.ResetFilter();
            var spriteDataType = GetComponentTypeHandle<SpriteData>(true);
            int renderedSpriteCount = 0;

            foreach (var group in renderGroups.Values)
            {
                filteredSpriteQuery.SetSharedComponentFilter(SpriteSheetRuntime.CreateRenderKey(group.SheetId));
                int spriteCount = filteredSpriteQuery.CalculateEntityCount();

                if (spriteCount == 0)
                {
                    group.ResetCount();
                    continue;
                }

                renderedSpriteCount += spriteCount;
                group.EnsureCapacity(spriteCount);
                group.ResetCount(spriteCount);

                var chunkBaseEntityIndices = filteredSpriteQuery.CalculateBaseEntityIndexArrayAsync(
                    Allocator.TempJob,
                    Dependency,
                    out JobHandle chunkBaseEntityIndicesHandle);

                var uploadHandle = new UploadSpriteDataJob
                {
                    SpriteDataType = spriteDataType,
                    ChunkBaseEntityIndices = chunkBaseEntityIndices,
                    TranslationAndRotationOutput = group.BeginTranslationAndRotationWrite(spriteCount),
                    ScaleOutput = group.BeginScaleWrite(spriteCount),
                    ColorOutput = group.BeginColorWrite(spriteCount),
                    FrameIndexOutput = group.BeginFrameIndexWrite(spriteCount),
                    MaxFrameIndex = math.max(0, group.FrameCount - 1)
                }.ScheduleParallel(filteredSpriteQuery, chunkBaseEntityIndicesHandle);

                uploadHandle.Complete();
                group.EndWrite();
                group.Draw(GetQuadMesh());
            }

            filteredSpriteQuery.ResetFilter();
            Dependency = default;

            if (renderedSpriteCount < totalSpriteCount)
            {
                if (!hasLoggedUnmatchedSprites)
                {
                    Debug.LogWarning("SpriteSystem found SpriteData entities that could not be rendered because they are missing SpriteSheetRenderKey or a matching SpriteSheetDefinition.");
                    hasLoggedUnmatchedSprites = true;
                }
            }
            else
            {
                hasLoggedUnmatchedSprites = false;
            }
        }

        protected override void OnDestroy()
        {
            foreach (var group in renderGroups.Values)
            {
                group.Dispose();
            }

            renderGroups.Clear();

            if (quadMesh != null)
            {
                DestroyUnityObject(quadMesh);
                quadMesh = null;
            }
        }

        private void BuildRenderGroups()
        {
            foreach (var group in renderGroups.Values)
            {
                group.Dispose();
            }

            renderGroups.Clear();

            var definitions = SpriteSheetDatabase.GetDefinitions();
            if (definitions == null || definitions.Length == 0)
            {
                lastDefinitionsSignature = SpriteSheetDatabase.GetDefinitionsSignature();
                hasLoggedMissingDefinitions = false;
                return;
            }

            foreach (var definition in definitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (definition.BaseMaterial == null)
                {
                    Debug.LogError($"SpriteSheetDefinition '{definition.name}' is missing a base material.");
                    continue;
                }

                if (renderGroups.ContainsKey(definition.SheetId))
                {
                    Debug.LogWarning($"Duplicate SpriteSheetId {definition.SheetId} detected. Skipping '{definition.name}'.");
                    continue;
                }

                renderGroups.Add(definition.SheetId, new SpriteRenderGroup(definition));
            }

            lastDefinitionsSignature = SpriteSheetDatabase.GetDefinitionsSignature();
            hasLoggedMissingDefinitions = false;
        }

        private Mesh GetQuadMesh()
        {
            if (quadMesh == null)
            {
                quadMesh = new Mesh
                {
                    name = "SpriteSystem Quad"
                };

                Vector3[] vertices =
                {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(1, 1, 0)
                };
                quadMesh.vertices = vertices;

                int[] triangles =
                {
                    0, 2, 1,
                    2, 3, 1
                };
                quadMesh.triangles = triangles;

                Vector3[] normals =
                {
                    -Vector3.forward,
                    -Vector3.forward,
                    -Vector3.forward,
                    -Vector3.forward
                };
                quadMesh.normals = normals;

                Vector2[] uv =
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1)
                };
                quadMesh.uv = uv;
                quadMesh.RecalculateBounds();
            }

            return quadMesh;
        }

        private sealed class SpriteRenderGroup : System.IDisposable
        {
            public readonly int SheetId;
            public readonly int CapacityStep;
            public readonly Vector4[] Frames;
            public readonly Bounds Bounds;
            public readonly Material Material;
            public readonly int FrameCount;
            public ComputeBuffer TranslationAndRotationBuffer;
            public ComputeBuffer ScaleBuffer;
            public ComputeBuffer ColorBuffer;
            public ComputeBuffer UvBuffer;
            public ComputeBuffer FrameIndexBuffer;
            public ComputeBuffer ArgsBuffer;
            public uint[] Args;
            public int Capacity;
            public int WriteIndex;

            public SpriteRenderGroup(SpriteSheetDefinition definition)
            {
                SheetId = definition.SheetId;
                CapacityStep = math.max(1, definition.CapacityStep);
                Bounds = definition.WorldBounds;
                Frames = definition.Frames;
                FrameCount = Frames.Length;
                Material = new Material(definition.BaseMaterial)
                {
                    name = $"{definition.name} (Runtime)"
                };
                Material.enableInstancing = true;

                if (definition.Texture != null)
                {
                    Material.mainTexture = definition.Texture;
                }

                CreateBuffers(math.max(1, definition.InitialCapacity));
                UploadUvData();
                BindBuffers();
            }

            public void ResetCount(int count = 0)
            {
                WriteIndex = count;
            }

            public void EnsureCapacity(int requiredCount)
            {
                if (requiredCount <= Capacity)
                {
                    return;
                }

                int nextCapacity = math.max(requiredCount, Capacity + CapacityStep);
                RecreateBuffers(nextCapacity);
            }

            public NativeArray<float4> BeginTranslationAndRotationWrite(int count)
                => TranslationAndRotationBuffer.BeginWrite<float4>(0, count);

            public NativeArray<float> BeginScaleWrite(int count)
                => ScaleBuffer.BeginWrite<float>(0, count);

            public NativeArray<float4> BeginColorWrite(int count)
                => ColorBuffer.BeginWrite<float4>(0, count);

            public NativeArray<int> BeginFrameIndexWrite(int count)
                => FrameIndexBuffer.BeginWrite<int>(0, count);

            public void EndWrite()
            {
                if (WriteIndex == 0)
                {
                    return;
                }

                TranslationAndRotationBuffer.EndWrite<float4>(WriteIndex);
                ScaleBuffer.EndWrite<float>(WriteIndex);
                ColorBuffer.EndWrite<float4>(WriteIndex);
                FrameIndexBuffer.EndWrite<int>(WriteIndex);

                Args[1] = (uint)WriteIndex;
                ArgsBuffer.SetData(Args);
            }

            public void Draw(Mesh mesh)
            {
                if (WriteIndex == 0)
                {
                    return;
                }

                Graphics.DrawMeshInstancedIndirect(mesh, 0, Material, Bounds, ArgsBuffer);
            }

            public void Dispose()
            {
                ReleaseBuffer(ref TranslationAndRotationBuffer);
                ReleaseBuffer(ref ScaleBuffer);
                ReleaseBuffer(ref ColorBuffer);
                ReleaseBuffer(ref UvBuffer);
                ReleaseBuffer(ref FrameIndexBuffer);
                ReleaseBuffer(ref ArgsBuffer);

                if (Material != null)
                {
                    SpriteSystem.DestroyUnityObject(Material);
                }
            }

            private void CreateBuffers(int capacity)
            {
                Capacity = capacity;
                Args = new uint[5] { 6, 0, 0, 0, 0 };

                TranslationAndRotationBuffer = new ComputeBuffer(capacity, 16, ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
                ScaleBuffer = new ComputeBuffer(capacity, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
                ColorBuffer = new ComputeBuffer(capacity, 16, ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
                FrameIndexBuffer = new ComputeBuffer(capacity, sizeof(int), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
                UvBuffer = new ComputeBuffer(math.max(1, FrameCount), 16);
                ArgsBuffer = new ComputeBuffer(1, Args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            }

            private void RecreateBuffers(int capacity)
            {
                ReleaseBuffer(ref TranslationAndRotationBuffer);
                ReleaseBuffer(ref ScaleBuffer);
                ReleaseBuffer(ref ColorBuffer);
                ReleaseBuffer(ref FrameIndexBuffer);
                ReleaseBuffer(ref ArgsBuffer);

                Capacity = capacity;
                Args = new uint[5] { 6, 0, 0, 0, 0 };

                TranslationAndRotationBuffer = new ComputeBuffer(capacity, 16, ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
                ScaleBuffer = new ComputeBuffer(capacity, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
                ColorBuffer = new ComputeBuffer(capacity, 16, ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
                FrameIndexBuffer = new ComputeBuffer(capacity, sizeof(int), ComputeBufferType.Default, ComputeBufferMode.SubUpdates);
                ArgsBuffer = new ComputeBuffer(1, Args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

                BindBuffers();
            }

            private void UploadUvData()
            {
                if (FrameCount > 0)
                {
                    UvBuffer.SetData(Frames);
                }
            }

            private void BindBuffers()
            {
                Material.SetBuffer(UvBufferId, UvBuffer);
                Material.SetBuffer(TranslationAndRotationBufferId, TranslationAndRotationBuffer);
                Material.SetBuffer(ScaleBufferId, ScaleBuffer);
                Material.SetBuffer(ColorsBufferId, ColorBuffer);
                Material.SetBuffer(FrameIndexBufferId, FrameIndexBuffer);
            }

            private static void ReleaseBuffer(ref ComputeBuffer buffer)
            {
                if (buffer != null)
                {
                    buffer.Release();
                    buffer = null;
                }
            }
        }

        private static void DestroyUnityObject(Object unityObject)
        {
            if (unityObject == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(unityObject);
            }
            else
            {
                Object.DestroyImmediate(unityObject);
            }
        }
    }
}
