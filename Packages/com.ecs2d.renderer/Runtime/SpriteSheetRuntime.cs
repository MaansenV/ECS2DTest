using Unity.Collections;
using Unity.Entities;

namespace ECS2D.Rendering
{
    public static class SpriteSheetRuntime
    {
        public static SpriteSheetRenderKey CreateRenderKey(int sheetId)
            => new SpriteSheetRenderKey { SheetId = sheetId };

        public static void SetSheet(EntityManager entityManager, Entity entity, int sheetId)
        {
            var spriteData = entityManager.GetComponentData<SpriteData>(entity);
            spriteData.SpriteSheetId = sheetId;
            entityManager.SetComponentData(entity, spriteData);
            entityManager.SetSharedComponent(entity, CreateRenderKey(sheetId));
        }

        public static void SetSheet(EntityManager entityManager, EntityQuery query, int sheetId)
        {
            using var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; i++)
            {
                SetSheet(entityManager, entities[i], sheetId);
            }
        }
    }
}
