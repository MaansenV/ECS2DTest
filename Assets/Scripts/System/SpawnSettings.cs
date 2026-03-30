using Unity.Entities;

namespace Systems
{
    public struct SpawnSettings : IComponentData
    {
        public bool Enabled;
        public int SpawnPerFrame;
        public int MaxSpriteCount;
    }
}
