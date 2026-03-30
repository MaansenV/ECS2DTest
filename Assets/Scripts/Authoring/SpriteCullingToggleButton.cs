using ECS2D.Rendering;
using Unity.Entities;
using UnityEngine;

namespace Authoring
{
    public class SpriteCullingToggleButton : MonoBehaviour
    {
        public bool RenderOnGui = true;
        public Rect ButtonRect = new Rect(16f, 16f, 180f, 36f);

        public void ToggleCulling()
        {
            SetCullingEnabled(!GetCurrentCullingEnabled());
        }

        public void SetCullingEnabled(bool enabled)
        {
            SpriteCullingRuntime.SetOverride(enabled);

            if (TryGetOrCreateSettingsEntity(out var entityManager, out var settingsEntity))
            {
                entityManager.SetComponentData(settingsEntity, new SpriteCullingSettings
                {
                    Enabled = enabled ? (byte)1 : (byte)0
                });
            }
        }

        private void OnGUI()
        {
            if (!RenderOnGui)
            {
                return;
            }

            bool isEnabled = GetCurrentCullingEnabled();
            string label = isEnabled ? "Culling: On" : "Culling: Off";

            if (GUI.Button(ButtonRect, label))
            {
                ToggleCulling();
            }
        }

        private static bool GetCurrentCullingEnabled()
        {
            if (SpriteCullingRuntime.TryGetOverride(out bool overrideEnabled))
            {
                return overrideEnabled;
            }

            if (!TryGetOrCreateSettingsEntity(out var entityManager, out var settingsEntity))
            {
                return true;
            }

            return entityManager.GetComponentData<SpriteCullingSettings>(settingsEntity).Enabled != 0;
        }

        private static bool TryGetOrCreateSettingsEntity(out EntityManager entityManager, out Entity settingsEntity)
        {
            entityManager = default;
            settingsEntity = Entity.Null;

            var world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
            {
                return false;
            }

            entityManager = world.EntityManager;
            var query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<SpriteCullingSettings>());

            try
            {
                if (query.IsEmptyIgnoreFilter)
                {
                    settingsEntity = entityManager.CreateEntity(typeof(SpriteCullingSettings));
                    entityManager.SetComponentData(settingsEntity, new SpriteCullingSettings
                    {
                        Enabled = 1
                    });
                    return true;
                }

                settingsEntity = query.GetSingletonEntity();
                return true;
            }
            finally
            {
                query.Dispose();
            }
        }
    }
}
