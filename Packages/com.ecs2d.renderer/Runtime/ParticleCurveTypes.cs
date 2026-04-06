using Unity.Entities;

namespace ECS2D.Rendering
{
    public enum ParticleCurveMode : byte
    {
        Constant = 0,
        Curve = 1
    }

    public struct CurveBlobLUT
    {
        public const int kSampleCount = 64;

        public BlobArray<float> Samples;
    }
}
