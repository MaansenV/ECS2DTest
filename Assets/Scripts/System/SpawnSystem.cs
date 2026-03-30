using ECS2D.Rendering;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Systems
{
    public partial struct SpawnSystem : ISystem
    {
        private Random _random;
        private EntityQuery _spriteQuery;
        private bool _warnedMissingSheets;

        private const int SpawnPerFrame = 10;
        private const int MaxSpriteCount = 500;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _random = new Random(555);
            _spriteQuery = state.GetEntityQuery(ComponentType.ReadOnly<SpriteData>());
            state.RequireForUpdate<EntitiesReferences>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var sheetDefinitions = SpriteSheetDatabase.GetDefinitions();
            if (sheetDefinitions == null || sheetDefinitions.Length == 0)
            {
                if (!_warnedMissingSheets)
                {
                    Debug.LogWarning("No SpriteSheetDefinition assets were found in Resources/SpriteSheets.");
                    _warnedMissingSheets = true;
                }

                return;
            }

            _warnedMissingSheets = false;

            var references = SystemAPI.GetSingleton<EntitiesReferences>();
            int currentSpriteCount = _spriteQuery.CalculateEntityCount();
            if (currentSpriteCount >= MaxSpriteCount)
            {
                return;
            }

            int spawnCount = math.min(SpawnPerFrame, MaxSpriteCount - currentSpriteCount);

            for (int i = 0; i < spawnCount; i++)
            {
                var sheetDefinition = sheetDefinitions[_random.NextInt(0, sheetDefinitions.Length)];
                if (sheetDefinition == null)
                {
                    continue;
                }

                var entity = state.EntityManager.Instantiate(references.BulletPrefab);
                var data = state.EntityManager.GetComponentData<SpriteData>(entity);
                data.TranslationAndRotation =
                    new float4(_random.NextFloat(-8.0f, 8.0f), _random.NextFloat(-8.0f, 8.0f), 0, 0);
                data.Scale = _random.NextFloat(0.2f, 0.6f);
                data.Color = new float4(1.0f, 1.0f, 1.0f, 1.0f);
                data.SpriteSheetId = sheetDefinition.SheetId;
                data.SpriteFrameIndex = _random.NextInt(0, math.max(1, sheetDefinition.FrameCount));
                state.EntityManager.SetComponentData(entity, data);
            }
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
