using Unity.Entities;
using UnityEngine;

namespace Systems
{
    public class SpawnSettingsAuthoring : MonoBehaviour
    {
        public bool Enabled = true;
        public int SpawnPerFrame = 10;
        public int MaxSpriteCount = 500;

        private class SpawnSettingsAuthoringBaker : Baker<SpawnSettingsAuthoring>
        {
            public override void Bake(SpawnSettingsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpawnSettings
                {
                    Enabled = authoring.Enabled,
                    SpawnPerFrame = Mathf.Max(0, authoring.SpawnPerFrame),
                    MaxSpriteCount = Mathf.Max(0, authoring.MaxSpriteCount)
                });
            }
        }
    }
}
