# CoreTests

`Assets/Scripts/Core`(Unity 非依存のゲームロジック)と `Assets/Tests/EditMode` のテストを、
Unity Editor なしで .NET 8 だけで実行するためのプロジェクトです。

```sh
dotnet test CoreTests/Garapon.CoreTests.csproj
```

- `UnityEngine` を使うテスト(`JsonUtility` の往復テストなど)は `#if UNITY_5_3_OR_NEWER` で囲んであり、ここでは実行されません。Unity の Test Runner では従来どおり全件実行されます。
- GitHub Actions(`.github/workflows/core-tests.yml`)で push / PR ごとに実行されます。
