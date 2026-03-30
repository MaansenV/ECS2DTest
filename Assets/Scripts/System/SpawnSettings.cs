using Unity.Entities;

namespace Systems
{
    public struct SpawnPrefabReferences : IComponentData
    {
        public Entity PrefabEntity;
    }

    public struct SpawnSettings : IComponentData
    {
        public bool Enabled;
        public int GridRows;
        public int GridColumns;
        public float SpriteSize;
        public float SpacingX;
        public float SpacingY;
        public int SpawnPerFrame;
    }
}
