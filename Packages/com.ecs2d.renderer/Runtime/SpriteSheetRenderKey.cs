using System;
using Unity.Entities;

namespace ECS2D.Rendering
{
    public struct SpriteSheetRenderKey : ISharedComponentData, IEquatable<SpriteSheetRenderKey>
    {
        public int SheetId;

        public bool Equals(SpriteSheetRenderKey other)
            => SheetId == other.SheetId;

        public override bool Equals(object obj)
            => obj is SpriteSheetRenderKey other && Equals(other);

        public override int GetHashCode()
            => SheetId;
    }
}
