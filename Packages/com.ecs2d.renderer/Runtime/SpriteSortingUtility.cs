using Unity.Mathematics;

namespace ECS2D.Rendering
{
    public static class SpriteSortingUtility
    {
        // Keep layer influence dominant over ordinary Y movement while staying
        // within a small render-depth range for typical 2D camera setups.
        public const float LayerStride = 100f;
        public const float VerticalScale = 0.01f;
        public const float SheetBiasScale = 0.0001f;
        public const float UploadIndexBiasScale = 0.000000001f;

        public static float CalculateRenderDepth(int sortingLayer, float verticalPosition, int sheetId)
        {
            int stableSheetBucket = math.abs(sheetId % 997);
            return (sortingLayer * LayerStride)
                 - (verticalPosition * VerticalScale)
                 + (stableSheetBucket * SheetBiasScale);
        }

        public static float ApplyUploadIndexBias(float renderDepth, int uploadIndex)
            => renderDepth + (uploadIndex * UploadIndexBiasScale);
    }
}
