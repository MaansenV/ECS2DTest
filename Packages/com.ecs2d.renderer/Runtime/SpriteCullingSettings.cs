using Unity.Entities;

namespace ECS2D.Rendering
{
    public struct SpriteCullingSettings : IComponentData
    {
        public byte Enabled;
    }

    public static class SpriteCullingRuntime
    {
        private static bool hasOverride;
        private static bool overrideEnabled = true;

        public static bool TryGetOverride(out bool enabled)
        {
            enabled = overrideEnabled;
            return hasOverride;
        }

        public static void SetOverride(bool enabled)
        {
            overrideEnabled = enabled;
            hasOverride = true;
        }

        public static void ClearOverride()
        {
            hasOverride = false;
        }
    }
}
