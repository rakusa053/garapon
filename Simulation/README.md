# Simulation

`Garapon.Core` のルールを使って「それなりに効率よく遊ぶプレイヤー」を動かし、ペース配分(何分で何が買えるか、何時間でクリアか)を測るツールです。

```sh
dotnet run --project Simulation -c Release                    # 今のバランス
dotnet run --project Simulation -c Release -- --no-automation # 自動化系アップグレードを無効にした比較
dotnet run --project Simulation -c Release -- --no-fun        # ラッキーごみ・コンボ・スタンプ・入荷をすべて外した比較
dotnet run --project Simulation -c Release -- --no-lucky      # 1つずつ外す: --no-combo / --no-lucky / --no-stamps / --no-stocking
dotnet run --project Simulation -c Release -- --no-perks      # 買い物・強化・周回: --no-perks / --no-souvenirs / --no-milestones / --no-synergy / --no-sale / --no-bulk
dotnet run --project Simulation -c Release -- --debug         # 2周目の各プレイを1行ずつ表示
dotnet run --project Simulation -- --write-asset              # GameData.CreateDefault() から Assets/Data/GameData.asset を書き直す
```

- 移動距離・ごみの密度・1回転の秒数などは `GaraponWorldBuilder` と `GaraponMachine` から見積もった概算です。絶対値より、バランス変更前後の比較に使ってください。
- プレイヤーの方針: 45秒ごみを拾う(1個ずつ拾ってコンボを伸ばし、出現したラッキーごみは60%の確率で拾う) → 換金 → 入荷済みのアップグレードを安い順に全部買う → 残金の半分で食料品を買う → 最大2分ガラポンを回す(自動モーターがあれば拾っている間に回る)。クリアしたら2周目も遊ぶ。
- `GameData.CreateDefault()` を変えたら `--write-asset` で asset を更新してください。`CoreTests` の `AssetMatchesCreateDefault` が食い違いを検出します。
