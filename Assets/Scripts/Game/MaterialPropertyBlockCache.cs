using UnityEngine;

namespace Garapon.Game
{
    public sealed class MaterialPropertyBlockCache
    {
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        MaterialPropertyBlock block;

        public void Apply(Renderer target, Color color)
        {
            block ??= new MaterialPropertyBlock();
            block.SetColor(BaseColor, color);
            target.SetPropertyBlock(block);
        }
    }
}
