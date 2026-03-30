using Unity.Entities;

namespace ECS2D.Rendering
{
    public struct EntitiesReferences : IComponentData
    {
        public Entity BulletPrefab;
    }
}
