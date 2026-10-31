using Garapon.Core;
using UnityEngine;

namespace Garapon.Game
{
    [CreateAssetMenu(menuName = "Garapon/Game Data")]
    public sealed class GameDataAsset : ScriptableObject
    {
        public GameData Data = new GameData();
    }
}
