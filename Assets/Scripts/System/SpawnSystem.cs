using ECS2D.Rendering;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Systems
{
    public partial struct SpawnSystem : ISystem
    {
        private bool _gridSpawned;
        private int _nextGridIndex;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _gridSpawned = false;
            _nextGridIndex = 0;
            state.RequireForUpdate<SpawnSettings>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var spawnSettings = SystemAPI.GetSingleton<SpawnSettings>();
            if (!spawnSettings.Enabled)
            {
                return;
            }

            var settingsEntity = SystemAPI.GetSingletonEntity<SpawnSettings>();
            if (!state.EntityManager.HasComponent<SpawnPrefabReferences>(settingsEntity))
            {
                return;
            }

            var prefabs = state.EntityManager.GetComponentData<SpawnPrefabReferences>(settingsEntity);
            if (prefabs.PrefabEntity == Entity.Null)
            {
                return;
            }

            if (!_gridSpawned)
            {
                int totalSprites = spawnSettings.GridRows * spawnSettings.GridColumns;
                int spawned = SpawnGridBatch(ref state, ref spawnSettings, prefabs.PrefabEntity, _nextGridIndex, totalSprites);

                _nextGridIndex += spawned;

                if (_nextGridIndex >= totalSprites)
                {
                    _gridSpawned = true;
                }
            }
        }

        private int SpawnGridBatch(ref SystemState state, ref SpawnSettings spawnSettings, Entity prefabEntity, int startIndex, int totalSprites)
        {
            int spawnCount = math.min(spawnSettings.SpawnPerFrame, totalSprites - startIndex);
            if (spawnCount <= 0)
            {
                return 0;
            }

            float stepX = spawnSettings.SpriteSize + spawnSettings.SpacingX;
            float stepY = spawnSettings.SpriteSize + spawnSettings.SpacingY;

            for (int index = startIndex; index < startIndex + spawnCount; index++)
            {
                int row = index / spawnSettings.GridColumns;
                int col = index % spawnSettings.GridColumns;
                float3 position = new float3(col * stepX, row * stepY, 0f);

                var entity = state.EntityManager.Instantiate(prefabEntity);
                var data = state.EntityManager.GetComponentData<SpriteData>(entity);
                float rotationRadians = data.TranslationAndRotation.w;
                quaternion rotation = quaternion.identity;
                float4x4 worldMatrix = float4x4.TRS(position, quaternion.RotateZ(rotationRadians), new float3(spawnSettings.SpriteSize));

                if (state.EntityManager.HasComponent<LocalTransform>(entity))
                {
                    var localTransform = state.EntityManager.GetComponentData<LocalTransform>(entity);
                    localTransform.Position = position;
                    localTransform.Scale = spawnSettings.SpriteSize;
                    rotation = localTransform.Rotation;
                    state.EntityManager.SetComponentData(entity, localTransform);
                    worldMatrix = float4x4.TRS(position, rotation, new float3(spawnSettings.SpriteSize));
                    rotationRadians = math.atan2(worldMatrix.c0.y, worldMatrix.c0.x);

                    if (math.abs(rotationRadians + math.PI) < 0.0001f)
                    {
                        rotationRadians = math.PI;
                    }
                }

                if (state.EntityManager.HasComponent<LocalToWorld>(entity))
                {
                    state.EntityManager.SetComponentData(entity, new LocalToWorld
                    {
                        Value = worldMatrix
                    });
                }

                data.TranslationAndRotation = new float4(position, rotationRadians);
                data.Scale = spawnSettings.SpriteSize;
                data.Color = new float4(1.0f, 1.0f, 1.0f, 1.0f);
                state.EntityManager.SetComponentData(entity, data);
            }

            return spawnCount;
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
}
