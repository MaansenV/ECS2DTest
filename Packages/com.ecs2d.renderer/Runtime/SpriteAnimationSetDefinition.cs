using System.Collections.Generic;
using UnityEngine;

namespace ECS2D.Rendering
{
    [CreateAssetMenu(fileName = "SpriteAnimationSetDefinition", menuName = "ECS2D/Rendering/Sprite Animation Set Definition")]
    public sealed class SpriteAnimationSetDefinition : ScriptableObject
    {
        public SpriteSheetDefinition SpriteSheet;
        public List<SpriteAnimationClip> Clips = new List<SpriteAnimationClip>();

        private void OnValidate()
        {
            if (Clips == null)
            {
                Clips = new List<SpriteAnimationClip>();
            }
        }
    }
}
