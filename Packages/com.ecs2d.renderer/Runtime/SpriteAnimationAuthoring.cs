using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace ECS2D.Rendering
{
    public class SpriteAnimationAuthoring : MonoBehaviour
    {
        public SpriteAnimationSetDefinition AnimationSet;
        public string StartAnimation = string.Empty;
        public float PlaybackSpeed = 1f;
        public bool PlayOnStart = true;

        private class SpriteAnimationAuthoringBaker : Baker<SpriteAnimationAuthoring>
        {
            private struct ValidatedClip
            {
                public string Name;
                public int Row;
                public int StartColumn;
                public int FrameCount;
                public float FrameRate;
                public bool Loop;
                public bool PingPong;
            }

            public override void Bake(SpriteAnimationAuthoring authoring)
            {
                if (authoring.AnimationSet == null)
                {
                    Debug.LogError($"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' is missing an AnimationSet reference.");
                    return;
                }

                if (authoring.AnimationSet.SpriteSheet == null)
                {
                    Debug.LogError($"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' uses an AnimationSet without a SpriteSheet.");
                    return;
                }

                DependsOn(authoring.transform);
                DependsOn(authoring.AnimationSet);
                DependsOn(authoring.AnimationSet.SpriteSheet);

                if (!authoring.AnimationSet.SpriteSheet.AutoGenerateGridFrames)
                {
                    Debug.LogError($"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' requires a grid-based SpriteSheetDefinition. Enable autoGenerateGridFrames on '{authoring.AnimationSet.SpriteSheet.name}'.");
                    return;
                }

                if (!TryCollectValidatedClips(authoring, out var validatedClips))
                {
                    return;
                }

                if (!TryBuildBlob(authoring.AnimationSet, validatedClips, out var blobReference))
                {
                    return;
                }

                AddBlobAsset(ref blobReference, out _);

                int startClipIndex = ResolveStartClipIndex(authoring.StartAnimation, validatedClips);
                ref SpriteAnimationSetBlob animationSet = ref blobReference.Value;
                ref readonly var startClipBlob = ref blobReference.Value.Clips[startClipIndex];
                int startFrameIndex = SpriteAnimationSetBlobUtility.ResolveSpriteFrameIndex(ref animationSet, in startClipBlob, 0);

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new SpriteAnimationSetReference
                {
                    Value = blobReference
                });

                AddComponent(entity, new SpriteAnimationState
                {
                    CurrentAnimation = startClipBlob.Name,
                    Time = 0f,
                    PlaybackSpeed = math.max(0f, authoring.PlaybackSpeed),
                    Playing = authoring.PlayOnStart,
                    CurrentFrameIndex = 0,
                    Flags = SpriteAnimationState.InitializedFlag
                });

                var spriteDataAuthoring = GetComponent<SpriteDataAuthoring>();
                if (spriteDataAuthoring != null)
                {
                    if (spriteDataAuthoring.SpriteSheet != null && spriteDataAuthoring.SpriteSheet != authoring.AnimationSet.SpriteSheet)
                    {
                        Debug.LogWarning(
                            $"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' uses AnimationSet '{authoring.AnimationSet.name}' with SpriteSheet '{authoring.AnimationSet.SpriteSheet.name}', " +
                            $"but {nameof(SpriteDataAuthoring)} is assigned to '{spriteDataAuthoring.SpriteSheet.name}'. The animation system will drive the final sheet id from the set.");
                    }

                    return;
                }

                Vector3 lossyScale = authoring.transform.lossyScale;
                if (math.abs(lossyScale.x - lossyScale.y) > 0.0001f)
                {
                    Debug.LogWarning($"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' uses non-uniform scale. The renderer will use X scale for a uniform sprite size.");
                }

                float rotationRadians = math.radians(authoring.transform.eulerAngles.z);
                float scale = lossyScale.x;
                Vector3 position = authoring.transform.position;

                AddComponent(entity, new SpriteData
                {
                    TranslationAndRotation = new float4(position.x, position.y, position.z, rotationRadians),
                    Scale = scale,
                    Color = new float4(1f, 1f, 1f, 1f),
                    SpriteFrameIndex = startFrameIndex,
                    SpriteSheetId = authoring.AnimationSet.SpriteSheet.SheetId
                });
            }

            private static bool TryCollectValidatedClips(SpriteAnimationAuthoring authoring, out List<ValidatedClip> validatedClips)
            {
                validatedClips = new List<ValidatedClip>();
                var clips = authoring.AnimationSet.Clips;
                if (clips == null || clips.Count == 0)
                {
                    Debug.LogError($"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' uses AnimationSet '{authoring.AnimationSet.name}', but it does not contain any clips.");
                    return false;
                }

                int sheetColumns = math.max(1, authoring.AnimationSet.SpriteSheet.Columns);
                int sheetRows = math.max(1, authoring.AnimationSet.SpriteSheet.Rows);
                int frameCount = math.max(1, authoring.AnimationSet.SpriteSheet.FrameCount);
                var seenNames = new HashSet<string>(StringComparer.Ordinal);

                for (int i = 0; i < clips.Count; i++)
                {
                    var clip = clips[i];
                    string clipName = clip.Name ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(clipName))
                    {
                        Debug.LogWarning($"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' has a clip without a name. That clip was skipped.");
                        continue;
                    }

                    if (!seenNames.Add(clipName))
                    {
                        Debug.LogWarning($"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' has a duplicate clip name '{clipName}'. The later clip was skipped.");
                        continue;
                    }

                    if (clip.FrameCount <= 0)
                    {
                        Debug.LogWarning($"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' has an empty clip '{clipName}'. That clip was skipped.");
                        continue;
                    }

                    if (clip.Row < 0 || clip.Row >= sheetRows)
                    {
                        Debug.LogError($"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' has clip '{clipName}' with row {clip.Row}, but the SpriteSheet only has {sheetRows} rows. That clip was skipped.");
                        continue;
                    }

                    if (clip.StartColumn < 0 || clip.StartColumn >= sheetColumns)
                    {
                        Debug.LogError($"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' has clip '{clipName}' with start column {clip.StartColumn}, but the SpriteSheet only has {sheetColumns} columns. That clip was skipped.");
                        continue;
                    }

                    int endColumn = clip.StartColumn + clip.FrameCount - 1;
                    if (endColumn >= sheetColumns)
                    {
                        Debug.LogError(
                            $"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' has clip '{clipName}' starting at column {clip.StartColumn} with length {clip.FrameCount}, " +
                            $"but the SpriteSheet only has {sheetColumns} columns. That clip was skipped.");
                        continue;
                    }

                    int startFrameIndex = (clip.Row * sheetColumns) + clip.StartColumn;
                    if (startFrameIndex < 0 || startFrameIndex + clip.FrameCount > frameCount)
                    {
                        Debug.LogError(
                            $"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' has clip '{clipName}' that resolves outside the SpriteSheet frame range. That clip was skipped.");
                        continue;
                    }

                    validatedClips.Add(new ValidatedClip
                    {
                        Name = clipName,
                        Row = clip.Row,
                        StartColumn = clip.StartColumn,
                        FrameCount = clip.FrameCount,
                        FrameRate = math.max(0f, clip.FrameRate),
                        Loop = clip.Loop,
                        PingPong = clip.PingPong
                    });
                }

                if (validatedClips.Count == 0)
                {
                    Debug.LogError($"{nameof(SpriteAnimationAuthoring)} on '{authoring.name}' did not contain any valid clips.");
                    return false;
                }

                return true;
            }

            private static bool TryBuildBlob(SpriteAnimationSetDefinition animationSet, IReadOnlyList<ValidatedClip> validatedClips, out BlobAssetReference<SpriteAnimationSetBlob> blobReference)
            {
                blobReference = default;

                using var builder = new BlobBuilder(Allocator.Temp);
                ref var root = ref builder.ConstructRoot<SpriteAnimationSetBlob>();
                root.SpriteSheetId = animationSet.SpriteSheet.SheetId;
                root.Columns = math.max(1, animationSet.SpriteSheet.Columns);

                var clipArray = builder.Allocate(ref root.Clips, validatedClips.Count);
                for (int i = 0; i < validatedClips.Count; i++)
                {
                    var clip = validatedClips[i];
                    clipArray[i] = new SpriteAnimationClipBlob
                    {
                        Name = (FixedString64Bytes)clip.Name,
                        Row = clip.Row,
                        StartColumn = clip.StartColumn,
                        FrameCount = clip.FrameCount,
                        FrameRate = clip.FrameRate,
                        Loop = clip.Loop ? (byte)1 : (byte)0,
                        PingPong = clip.PingPong ? (byte)1 : (byte)0
                    };
                }

                blobReference = builder.CreateBlobAssetReference<SpriteAnimationSetBlob>(Allocator.Persistent);
                return true;
            }

            private static int ResolveStartClipIndex(string startAnimation, IReadOnlyList<ValidatedClip> validatedClips)
            {
                if (string.IsNullOrWhiteSpace(startAnimation))
                {
                    return 0;
                }

                for (int i = 0; i < validatedClips.Count; i++)
                {
                    if (string.Equals(validatedClips[i].Name, startAnimation, StringComparison.Ordinal))
                    {
                        return i;
                    }
                }

                Debug.LogWarning($"Requested start animation '{startAnimation}' was not found. Falling back to the first valid clip.");
                return 0;
            }

        }
    }
}
