using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace ECS2D.Rendering
{
    [Serializable]
    public struct SpriteAnimationClip
    {
        public string Name;
        public int Row;
        public int StartColumn;
        public int FrameCount;
        public float FrameRate;
        public bool Loop;
        public bool PingPong;
    }

    public struct SpriteAnimationClipBlob
    {
        public FixedString64Bytes Name;
        public int Row;
        public int StartColumn;
        public int FrameCount;
        public float FrameRate;
        public byte Loop;
        public byte PingPong;
    }

    public struct SpriteAnimationSetBlob
    {
        public int SpriteSheetId;
        public int Columns;
        public BlobArray<SpriteAnimationClipBlob> Clips;
    }

    internal static class SpriteAnimationSetBlobUtility
    {
        public static bool TryGetClipIndex(ref SpriteAnimationSetBlob set, in FixedString64Bytes animationName, out int clipIndex)
        {
            for (int i = 0; i < set.Clips.Length; i++)
            {
                if (set.Clips[i].Name.Equals(animationName))
                {
                    clipIndex = i;
                    return true;
                }
            }

            clipIndex = -1;
            return false;
        }

        public static int EvaluateFrameIndex(in SpriteAnimationClipBlob clip, float time, float playbackSpeed)
        {
            int frameCount = clip.FrameCount;
            if (frameCount <= 1 || clip.FrameRate <= 0f || playbackSpeed <= 0f)
            {
                return 0;
            }

            float frameProgress = time * clip.FrameRate * playbackSpeed;
            int step = (int)math.floor(frameProgress);
            if (step < 0)
            {
                step = 0;
            }

            if (clip.PingPong != 0 && frameCount > 1)
            {
                int cycleLength = (frameCount * 2) - 2;
                if (cycleLength <= 0)
                {
                    return 0;
                }

                if (clip.Loop != 0)
                {
                    int cycleStep = step % cycleLength;
                    if (cycleStep < 0)
                    {
                        cycleStep += cycleLength;
                    }

                    return cycleStep < frameCount ? cycleStep : cycleLength - cycleStep;
                }

                int clampedStep = math.min(step, cycleLength - 1);
                return clampedStep < frameCount ? clampedStep : cycleLength - clampedStep;
            }

            if (clip.Loop != 0)
            {
                return step % frameCount;
            }

            return math.min(step, frameCount - 1);
        }

        public static int ResolveSpriteFrameIndex(ref SpriteAnimationSetBlob set, in SpriteAnimationClipBlob clip, int clipFrameIndex)
        {
            if (clip.FrameCount <= 0 || set.Columns <= 0)
            {
                return 0;
            }

            int safeClipFrameIndex = math.clamp(clipFrameIndex, 0, clip.FrameCount - 1);
            int frameIndex = (clip.Row * set.Columns) + clip.StartColumn + safeClipFrameIndex;
            if (frameIndex < 0)
            {
                return 0;
            }

            return frameIndex;
        }
    }
}
