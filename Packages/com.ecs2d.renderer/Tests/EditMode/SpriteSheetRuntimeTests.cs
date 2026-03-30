using NUnit.Framework;
using Unity.Entities;

namespace ECS2D.Rendering.Tests
{
    public sealed class SpriteSheetRuntimeTests
    {
        [Test]
        public void SetSheet_UpdatesSpriteDataAndSharedRenderKey()
        {
            using var world = new World("SpriteSheetRuntimeTests");
            var entityManager = world.EntityManager;
            var entity = entityManager.CreateEntity(typeof(SpriteData), typeof(SpriteSheetRenderKey));

            entityManager.SetComponentData(entity, new SpriteData
            {
                SpriteSheetId = 1
            });
            entityManager.SetSharedComponent(entity, new SpriteSheetRenderKey
            {
                SheetId = 1
            });

            SpriteSheetRuntime.SetSheet(entityManager, entity, 7);

            var spriteData = entityManager.GetComponentData<SpriteData>(entity);
            var renderKey = entityManager.GetSharedComponent<SpriteSheetRenderKey>(entity);

            Assert.AreEqual(7, spriteData.SpriteSheetId);
            Assert.AreEqual(7, renderKey.SheetId);
        }
    }
}
