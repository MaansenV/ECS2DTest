using Unity.Entities;
using UnityEngine;

namespace ECS2D.Rendering
{
    public class EntitiesReferenceAuthoring : MonoBehaviour
    {
        [Tooltip("Prefab converted into the BulletPrefab entity reference.")]
        public GameObject bulletPrefab;

        private class EntitiesReferenceAuthoringBaker : Baker<EntitiesReferenceAuthoring>
        {
            public override void Bake(EntitiesReferenceAuthoring authoring)
            {
                var e = GetEntity(TransformUsageFlags.Dynamic);
                var bulletPrefab = GetEntity(authoring.bulletPrefab, TransformUsageFlags.Dynamic);

                AddComponent(e, new EntitiesReferences
                {
                    BulletPrefab = bulletPrefab,
                });
            }
        }
    }
}
