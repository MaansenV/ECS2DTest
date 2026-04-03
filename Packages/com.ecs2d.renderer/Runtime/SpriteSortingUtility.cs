using Unity.Mathematics;

namespace ECS2D.Rendering
{
    public static class SpriteSortingUtility
    {
        // Layer/Y sorting is encoded as a small render-depth offset around the
        // sprite's actual world Z so sprites remain in front of the camera.
        public const float LayerStride = 0.1f;
        public const float VerticalScale = 0.0001f;
        public const float SheetBiasScale = 0.000001f;
        public const float UploadIndexBiasScale = 0.000000001f;

        public static float CalculateRenderDepth(int sortingLayer, float verticalPosition, int sheetId, float worldZ = 0f)
        {
            int stableSheetBucket = math.abs(sheetId % 997);
            return worldZ
                 - (sortingLayer * LayerStride)
                 + (verticalPosition * VerticalScale)
                 - (stableSheetBucket * SheetBiasScale);
        }

        public static float ApplyUploadIndexBias(float renderDepth, int uploadIndex)
            => renderDepth - (uploadIndex * UploadIndexBiasScale);
    }
}
