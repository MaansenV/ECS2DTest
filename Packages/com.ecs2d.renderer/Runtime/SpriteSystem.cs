using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS2D.Rendering
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class SpriteSystem : SystemBase
    {
        private EntityQuery spriteQuery;
        private readonly Dictionary<int, SpriteRenderGroup> renderGroups = new Dictionary<int, SpriteRenderGroup>();
        private readonly Dictionary<int, int> sheetCounts = new Dictionary<int, int>();
        private readonly HashSet<int> missingSheetWarnings = new HashSet<int>();
        private Mesh quadMesh;
        private bool hasLoggedMissingDefinitions;

        private static readonly int TranslationAndRotationBufferId = Shader.PropertyToID("translationAndRotationBuffer");
        private static readonly int ScaleBufferId = Shader.PropertyToID("scaleBuffer");
        private static readonly int ColorsBufferId = Shader.PropertyToID("colorsBuffer");
        private static readonly int UvBufferId = Shader.PropertyToID("uvBuffer");
        private static readonly int FrameIndexBufferId = Shader.PropertyToID("frameIndexBuffer");

        protected override void OnCreate()
        {
            spriteQuery = GetEntityQuery(ComponentType.ReadOnly<SpriteData>());
        }

        protected override void OnStartRunning()
        {
            BuildRenderGroups();
        }

        protected override void OnUpdate()
        {
            if (renderGroups.Count == 0)
            {
                if (!hasLoggedMissingDefinitions)
                {
                    Debug.LogWarning("SpriteSystem did not find any SpriteSheetDefinition assets in Resources/SpriteSheets.");
                    hasLoggedMissingDefinitions = true;
                }

                return;
            }

            int spriteCount = spriteQuery.CalculateEntityCount();
            if (spriteCount == 0)
            {
                return;
            }

            var spriteDataArray = spriteQuery.ToComponentDataArray<SpriteData>(Allocator.TempJob);
            try
            {
                sheetCounts.Clear();
                missingSheetWarnings.Clear();

                for (int i = 0; i < spriteDataArray.Length; i++)
                {
                    var spriteData = spriteDataArray[i];
                    if (!sheetCounts.TryGetValue(spriteData.SpriteSheetId, out int count))
                    {
                        sheetCounts[spriteData.SpriteSheetId] = 1;
                    }
                    else
                    {
                        sheetCounts[spriteData.SpriteSheetId] = count + 1;
                    }
                }

                foreach (var sheetCount in sheetCounts)
                {
                    if (!renderGroups.TryGetValue(sheetCount.Key, out var group))
                    {
                        if (missingSheetWarnings.Add(sheetCount.Key))
                        {
                            Debug.LogWarning($"SpriteSystem found SpriteData using SpriteSheetId {sheetCount.Key}, but no matching SpriteSheetDefinition is loaded.");
                        }

                        continue;
                    }

                    group.EnsureCapacity(sheetCount.Value);
                    group.ResetCount();
                }

                for (int i = 0; i < spriteDataArray.Length; i++)
                {
                    var spriteData = spriteDataArray[i];
                    if (!renderGroups.TryGetValue(spriteData.SpriteSheetId, out var group))
                    {
                        continue;
                    }

                    if (group.FrameCount == 0)
                    {
                        continue;
                    }

                    int writeIndex = group.WriteIndex++;
                    int frameIndex = math.clamp(spriteData.SpriteFrameIndex, 0, group.FrameCount - 1);
                    group.TranslationAndRotationData[writeIndex] = spriteData.TranslationAndRotation;
                    group.ScaleData[writeIndex] = spriteData.Scale;
                    group.ColorData[writeIndex] = spriteData.Color;
                    group.FrameIndexData[writeIndex] = frameIndex;
                }

                foreach (var group in renderGroups.Values)
                {
                    if (group.WriteIndex == 0)
                    {
                        continue;
                    }

                    group.Upload();
                    Graphics.DrawMeshInstancedIndirect(GetQuadMesh(), 0, group.Material, group.Bounds, group.ArgsBuffer);
                }
            }
            finally
            {
                spriteDataArray.Dispose();
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
                Object.Destroy(quadMesh);
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
            public float4[] TranslationAndRotationData;
            public float[] ScaleData;
            public float4[] ColorData;
            public int[] FrameIndexData;
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

            public void ResetCount()
            {
                WriteIndex = 0;
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

            public void Upload()
            {
                TranslationAndRotationBuffer.SetData(TranslationAndRotationData, 0, 0, WriteIndex);
                ScaleBuffer.SetData(ScaleData, 0, 0, WriteIndex);
                ColorBuffer.SetData(ColorData, 0, 0, WriteIndex);
                FrameIndexBuffer.SetData(FrameIndexData, 0, 0, WriteIndex);

                Args[1] = (uint)WriteIndex;
                ArgsBuffer.SetData(Args);
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
                    Object.Destroy(Material);
                }
            }

            private void CreateBuffers(int capacity)
            {
                Capacity = capacity;
                TranslationAndRotationData = new float4[capacity];
                ScaleData = new float[capacity];
                ColorData = new float4[capacity];
                FrameIndexData = new int[capacity];
                Args = new uint[5] { 6, 0, 0, 0, 0 };

                TranslationAndRotationBuffer = new ComputeBuffer(capacity, 16);
                ScaleBuffer = new ComputeBuffer(capacity, sizeof(float));
                ColorBuffer = new ComputeBuffer(capacity, 16);
                FrameIndexBuffer = new ComputeBuffer(capacity, sizeof(int));
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

                CreateBuffers(capacity);
                UploadUvData();
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
    }
}
