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
        private bool needsRenderGroupRebuild;
        private int lastDefinitionsSignature = int.MinValue;

        private static readonly int TranslationAndRotationBufferId = Shader.PropertyToID("translationAndRotationBuffer");
        private static readonly int ScaleBufferId = Shader.PropertyToID("scaleBuffer");
        private static readonly int ColorsBufferId = Shader.PropertyToID("colorsBuffer");
        private static readonly int UvBufferId = Shader.PropertyToID("uvBuffer");
        private static readonly int FrameIndexBufferId = Shader.PropertyToID("frameIndexBuffer");
        private static readonly int FlipBufferId = Shader.PropertyToID("flipBuffer");
        private static readonly int RenderDepthBufferId = Shader.PropertyToID("renderDepthBuffer");

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
            [NativeDisableParallelForRestriction] public NativeArray<float2> FlipOutput;
            [NativeDisableParallelForRestriction] public NativeArray<float> RenderDepthOutput;
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
                FlipOutput[outputIndex] = new float2(spriteData.FlipX, spriteData.FlipY);
                RenderDepthOutput[outputIndex] = SpriteSortingUtility.ApplyUploadIndexBias(spriteData.RenderDepth, outputIndex);
            }
        }

        protected override void OnCreate()
        {
            allSpriteQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpriteData>(),
                ComponentType.ReadOnly<SpriteCullState>());
            filteredSpriteQuery = GetEntityQuery(
                ComponentType.ReadOnly<SpriteData>(),
                ComponentType.ReadOnly<SpriteSheetRenderKey>(),
                ComponentType.ReadOnly<SpriteCullState>());
        }

        protected override void OnStartRunning()
        {
            BuildRenderGroups();
        }

        protected override void OnUpdate()
        {
            int currentDefinitionsSignature = SpriteSheetDatabase.GetDefinitionsSignature();
            if (needsRenderGroupRebuild || currentDefinitionsSignature != lastDefinitionsSignature)
            {
                BuildRenderGroups();
                needsRenderGroupRebuild = false;
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
                group.AdvanceFrame();

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
                    FlipOutput = group.BeginFlipWrite(spriteCount),
                    RenderDepthOutput = group.BeginRenderDepthWrite(spriteCount),
                    MaxFrameIndex = math.max(0, group.FrameCount - 1)
                }.ScheduleParallel(filteredSpriteQuery, chunkBaseEntityIndicesHandle);

                uploadHandle.Complete();
                group.EndWrite();
                if (!group.Draw(GetQuadMesh()))
                {
                    needsRenderGroupRebuild = true;
                    return;
                }
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
            private const int UploadBufferCount = 3;

            private struct FrameBuffers
            {
                public ComputeBuffer TranslationAndRotationBuffer;
                public ComputeBuffer ScaleBuffer;
                public ComputeBuffer ColorBuffer;
                public ComputeBuffer FrameIndexBuffer;
                public ComputeBuffer FlipBuffer;
                public ComputeBuffer RenderDepthBuffer;
                public ComputeBuffer ArgsBuffer;
                public uint[] Args;
            }

            public readonly int SheetId;
            public readonly int CapacityStep;
            public readonly Vector4[] Frames;
            public readonly Bounds Bounds;
            public Material Material;
            public readonly int FrameCount;
            private readonly FrameBuffers[] frameBuffers = new FrameBuffers[UploadBufferCount];
            public ComputeBuffer UvBuffer;
            public int Capacity;
            public int WriteIndex;
            private int activeFrameBufferIndex;
            private bool isDisposed;

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
                => frameBuffers[activeFrameBufferIndex].TranslationAndRotationBuffer.BeginWrite<float4>(0, count);

            public NativeArray<float> BeginScaleWrite(int count)
                => frameBuffers[activeFrameBufferIndex].ScaleBuffer.BeginWrite<float>(0, count);

            public NativeArray<float4> BeginColorWrite(int count)
                => frameBuffers[activeFrameBufferIndex].ColorBuffer.BeginWrite<float4>(0, count);

            public NativeArray<int> BeginFrameIndexWrite(int count)
                => frameBuffers[activeFrameBufferIndex].FrameIndexBuffer.BeginWrite<int>(0, count);

            public NativeArray<float2> BeginFlipWrite(int count)
                => frameBuffers[activeFrameBufferIndex].FlipBuffer.BeginWrite<float2>(0, count);

            public NativeArray<float> BeginRenderDepthWrite(int count)
                => frameBuffers[activeFrameBufferIndex].RenderDepthBuffer.BeginWrite<float>(0, count);

            public void AdvanceFrame()
            {
                activeFrameBufferIndex = (activeFrameBufferIndex + 1) % UploadBufferCount;
            }

            public void EndWrite()
            {
                if (WriteIndex == 0)
                {
                    return;
                }

                ref FrameBuffers activeFrameBuffers = ref frameBuffers[activeFrameBufferIndex];

                activeFrameBuffers.TranslationAndRotationBuffer.EndWrite<float4>(WriteIndex);
                activeFrameBuffers.ScaleBuffer.EndWrite<float>(WriteIndex);
                activeFrameBuffers.ColorBuffer.EndWrite<float4>(WriteIndex);
                activeFrameBuffers.FrameIndexBuffer.EndWrite<int>(WriteIndex);
                activeFrameBuffers.FlipBuffer.EndWrite<float2>(WriteIndex);
                activeFrameBuffers.RenderDepthBuffer.EndWrite<float>(WriteIndex);

                activeFrameBuffers.Args[1] = (uint)WriteIndex;
                activeFrameBuffers.ArgsBuffer.SetData(activeFrameBuffers.Args);
            }

            public bool Draw(Mesh mesh)
            {
                if (WriteIndex == 0)
                {
                    return true;
                }

                if (!BindBuffers(frameBuffers[activeFrameBufferIndex]))
                {
                    return false;
                }

                Graphics.DrawMeshInstancedIndirect(mesh, 0, Material, Bounds, frameBuffers[activeFrameBufferIndex].ArgsBuffer);
                return true;
            }

            public void Dispose()
            {
                if (isDisposed)
                {
                    return;
                }

                isDisposed = true;

                for (int i = 0; i < frameBuffers.Length; i++)
                {
                    ReleaseFrameBuffers(ref frameBuffers[i]);
                }

                ReleaseBuffer(ref UvBuffer);

                if (Material != null)
                {
                    SpriteSystem.DestroyUnityObject(Material);
                    Material = null;
                }

                WriteIndex = 0;
            }

            private void CreateBuffers(int capacity)
            {
                Capacity = capacity;
                for (int i = 0; i < frameBuffers.Length; i++)
                {
                    frameBuffers[i] = CreateFrameBuffers(capacity);
                }

                UvBuffer = new ComputeBuffer(math.max(1, FrameCount), 16);
            }

            private void RecreateBuffers(int capacity)
            {
                Capacity = capacity;
                for (int i = 0; i < frameBuffers.Length; i++)
                {
                    ReleaseFrameBuffers(ref frameBuffers[i]);
                    frameBuffers[i] = CreateFrameBuffers(capacity);
                }
            }

            private void UploadUvData()
            {
                if (FrameCount > 0)
                {
                    UvBuffer.SetData(Frames);
                }
            }

            private bool BindBuffers(in FrameBuffers buffers)
            {
                if (isDisposed || Material == null || UvBuffer == null || buffers.ArgsBuffer == null)
                {
                    return false;
                }

                Material.SetBuffer(UvBufferId, UvBuffer);
                Material.SetBuffer(TranslationAndRotationBufferId, buffers.TranslationAndRotationBuffer);
                Material.SetBuffer(ScaleBufferId, buffers.ScaleBuffer);
                Material.SetBuffer(ColorsBufferId, buffers.ColorBuffer);
                Material.SetBuffer(FrameIndexBufferId, buffers.FrameIndexBuffer);
                Material.SetBuffer(FlipBufferId, buffers.FlipBuffer);
                Material.SetBuffer(RenderDepthBufferId, buffers.RenderDepthBuffer);
                return true;
            }

            private static FrameBuffers CreateFrameBuffers(int capacity)
            {
                return new FrameBuffers
                {
                    Args = new uint[5] { 6, 0, 0, 0, 0 },
                    TranslationAndRotationBuffer = new ComputeBuffer(capacity, 16, ComputeBufferType.Default, ComputeBufferMode.SubUpdates),
                    ScaleBuffer = new ComputeBuffer(capacity, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates),
                    ColorBuffer = new ComputeBuffer(capacity, 16, ComputeBufferType.Default, ComputeBufferMode.SubUpdates),
                    FrameIndexBuffer = new ComputeBuffer(capacity, sizeof(int), ComputeBufferType.Default, ComputeBufferMode.SubUpdates),
                    FlipBuffer = new ComputeBuffer(capacity, 8, ComputeBufferType.Default, ComputeBufferMode.SubUpdates),
                    RenderDepthBuffer = new ComputeBuffer(capacity, sizeof(float), ComputeBufferType.Default, ComputeBufferMode.SubUpdates),
                    ArgsBuffer = new ComputeBuffer(1, 5 * sizeof(uint), ComputeBufferType.IndirectArguments)
                };
            }

            private static void ReleaseFrameBuffers(ref FrameBuffers buffers)
            {
                ReleaseBuffer(ref buffers.TranslationAndRotationBuffer);
                ReleaseBuffer(ref buffers.ScaleBuffer);
                ReleaseBuffer(ref buffers.ColorBuffer);
                ReleaseBuffer(ref buffers.FrameIndexBuffer);
                ReleaseBuffer(ref buffers.FlipBuffer);
                ReleaseBuffer(ref buffers.RenderDepthBuffer);
                ReleaseBuffer(ref buffers.ArgsBuffer);
                buffers.Args = null;
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
