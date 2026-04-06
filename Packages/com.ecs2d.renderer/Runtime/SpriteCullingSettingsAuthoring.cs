using Unity.Entities;
using UnityEngine;

namespace ECS2D.Rendering
{
    public class SpriteCullingSettingsAuthoring : MonoBehaviour
    {
        [Tooltip("Enable or disable sprite frustum culling for this scene.")]
        public bool CullingEnabled = true;

        private sealed class Baker : Baker<SpriteCullingSettingsAuthoring>
        {
            public override void Bake(SpriteCullingSettingsAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new SpriteCullingSettings
                {
                    Enabled = authoring.CullingEnabled ? (byte)1 : (byte)0
                });
            }
        }
    }
}
