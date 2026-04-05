using Unity.Entities;
using UnityEngine;

namespace Systems
{
    [DisallowMultipleComponent]
    public class SpawnAuthoring : MonoBehaviour
    {
        [Header("General")]
        public bool Enabled = true;
        public GameObject Prefab;

        [Header("Emitter")]
        public GameObject EmitterPrefab;

        [Header("Grid Spawn")]
        public int GridRows = 100;
        public int GridColumns = 100;
        public float SpriteSize = 0.5f;
        public float SpacingX = 0.1f;
        public float SpacingY = 0.1f;

        [Header("Performance")]
        public int SpawnPerFrame = 10000;

        private class SpawnAuthoringBaker : Baker<SpawnAuthoring>
    {
        public override void Bake(SpawnAuthoring authoring)
        {
            if (authoring.Prefab != null)
            {
                DependsOn(authoring.Prefab);
            }

            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new SpawnSettings
            {
                Enabled = authoring.Enabled,
                GridRows = Mathf.Max(1, authoring.GridRows),
                GridColumns = Mathf.Max(1, authoring.GridColumns),
                SpriteSize = Mathf.Max(0.01f, authoring.SpriteSize),
                SpacingX = Mathf.Max(0f, authoring.SpacingX),
                SpacingY = Mathf.Max(0f, authoring.SpacingY),
                SpawnPerFrame = Mathf.Max(1, authoring.SpawnPerFrame)
            });

            var prefabEntity = Entity.Null;
            if (authoring.Prefab != null)
            {
                prefabEntity = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic);
            }

            var emitterPrefabEntity = Entity.Null;
            if (authoring.EmitterPrefab != null)
            {
                DependsOn(authoring.EmitterPrefab);
                emitterPrefabEntity = GetEntity(authoring.EmitterPrefab, TransformUsageFlags.Dynamic);
            }

            AddComponent(entity, new SpawnPrefabReferences
            {
                PrefabEntity = prefabEntity,
                EmitterPrefabEntity = emitterPrefabEntity
            });
        }
    }
}
}
