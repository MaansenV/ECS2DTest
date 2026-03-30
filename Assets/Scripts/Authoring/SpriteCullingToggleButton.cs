using ECS2D.Rendering;
using Unity.Collections;
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

            if (TryGetExistingSettingsEntities(out var entityManager, out var settingsEntities))
            {
                var settingsData = new SpriteCullingSettings
                {
                    Enabled = enabled ? (byte)1 : (byte)0
                };

                for (int i = 0; i < settingsEntities.Length; i++)
                {
                    entityManager.SetComponentData(settingsEntities[i], settingsData);
                }

                settingsEntities.Dispose();
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

            if (!TryGetExistingSettingsEntities(out var entityManager, out var settingsEntities))
            {
                return true;
            }

            try
            {
                return entityManager.GetComponentData<SpriteCullingSettings>(settingsEntities[0]).Enabled != 0;
            }
            finally
            {
                settingsEntities.Dispose();
            }
        }

        private static bool TryGetExistingSettingsEntities(out EntityManager entityManager, out NativeArray<Entity> settingsEntities)
        {
            entityManager = default;
            settingsEntities = default;

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
                    return false;
                }

                settingsEntities = query.ToEntityArray(Allocator.Temp);
                return true;
            }
            finally
            {
                query.Dispose();
            }
        }
    }
}
