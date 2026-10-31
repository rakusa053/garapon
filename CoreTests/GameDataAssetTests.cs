using System.IO;
using System.Runtime.CompilerServices;
using Garapon.Core;
using Garapon.Tools;
using NUnit.Framework;

namespace Garapon.Tests
{
    // The game reads its numbers from Assets/Data/GameData.asset, not from GameData.CreateDefault().
    // This keeps the two in step; regenerate the asset with:
    //   dotnet run --project Simulation -- --write-asset
    public class GameDataAssetTests
    {
        static string AssetPath([CallerFilePath] string here = "") =>
            Path.Combine(Path.GetDirectoryName(here), "..", "Assets", "Data", "GameData.asset");

        [Test]
        public void AssetMatchesCreateDefault()
        {
            var asset = File.ReadAllText(AssetPath()).Replace("\r\n", "\n");
            var expected = GameDataYaml.ReplaceData(asset, GameData.CreateDefault());
            Assert.AreEqual(expected, asset, "GameData.asset is out of date. Run: dotnet run --project Simulation -- --write-asset");
        }
    }
}
