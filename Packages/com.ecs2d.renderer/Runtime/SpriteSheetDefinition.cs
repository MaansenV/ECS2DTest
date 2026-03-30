using System;
using UnityEngine;

namespace ECS2D.Rendering
{
    [CreateAssetMenu(fileName = "SpriteSheetDefinition", menuName = "ECS2D/Rendering/Sprite Sheet Definition")]
    public sealed class SpriteSheetDefinition : ScriptableObject
    {
        [SerializeField] private int sheetId = 1;
        [SerializeField] private Material baseMaterial;
        [SerializeField] private Texture2D texture;
        [SerializeField] private Bounds worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        [SerializeField] private int initialCapacity = 256;
        [SerializeField] private int capacityStep = 256;
        [SerializeField] private bool autoGenerateGridFrames = true;
        [SerializeField] private int columns = 4;
        [SerializeField] private int rows = 4;
        [SerializeField] private Vector4[] frames = Array.Empty<Vector4>();

        public int SheetId => sheetId;
        public Material BaseMaterial => baseMaterial;
        public Texture2D Texture => texture;
        public Bounds WorldBounds => worldBounds;
        public int InitialCapacity => Mathf.Max(1, initialCapacity);
        public int CapacityStep => Mathf.Max(1, capacityStep);
        public bool AutoGenerateGridFrames => autoGenerateGridFrames;
        public int Columns => Mathf.Max(1, columns);
        public int Rows => Mathf.Max(1, rows);
        public int FrameCount => Frames.Length;
        public Vector4[] Frames => GetFrames();

        private Vector4[] GetFrames()
        {
            if (!autoGenerateGridFrames)
            {
                if (frames == null || frames.Length == 0)
                {
                    frames = new[] { new Vector4(1f, 1f, 0f, 0f) };
                }

                return frames;
            }

            frames = BuildGridFrames(Columns, Rows);
            return frames;
        }

        private static Vector4[] BuildGridFrames(int columns, int rows)
        {
            var generatedFrames = new Vector4[columns * rows];
            float stepX = 1f / columns;
            float stepY = 1f / rows;

            for (int y = 0; y < rows; y++)
            {
                int atlasY = rows - 1 - y;

                for (int x = 0; x < columns; x++)
                {
                    int index = y * columns + x;
                    generatedFrames[index] = new Vector4(stepX, stepY, x * stepX, atlasY * stepY);
                }
            }

            return generatedFrames;
        }

        private void OnValidate()
        {
            sheetId = Mathf.Max(0, sheetId);
            initialCapacity = Mathf.Max(1, initialCapacity);
            capacityStep = Mathf.Max(1, capacityStep);
            columns = Mathf.Max(1, columns);
            rows = Mathf.Max(1, rows);

            _ = Frames;

            if (worldBounds.size == Vector3.zero)
            {
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
            }

#if UNITY_EDITOR
            SpriteSheetDatabase.RefreshCache();
#endif
        }
    }
}
