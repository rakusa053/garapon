# インクリメンタルゲームの面白さを増やす(2026-10-31)

## インクリメンタルゲームの面白さ(調査まとめ)

| 面白さ | 代表例 | 中身 |
|---|---|---|
| **発見** | A Dark Room、Universal Paperclips | 最初は画面も仕組みも少なく、遊ぶほど新しい要素が現れる。「次に何が出てくるのか」という好奇心が最大の原動力、という分析が多い |
| **気づいた人へのご褒美** | Cookie Clicker のゴールデンクッキー | 不定期に現れ、数秒で消える。拾うとランダムで「一定時間 生産×7(Frenzy)」「貯金の一部を即入手(Lucky)」など。放置ゲームに能動的に見る理由を作る |
| **指数的な成長の誇張** | ほぼ全作品 | 数字が桁違いに増えていく気持ちよさ(前回の対応で実装済み) |
| **実績に実利がある** | Cookie Clicker(実績でミルク→生産ボーナス) | バッジ集めがそのまま強さになるので、寄り道が報われる |
| **プレステージ(周回)** | AdVenture Capitalist ほか | リセットと引き換えに永続ボーナス。2周目は同じ道を速く駆け抜ける気持ちよさがあり、「いつリセットするか」が選択になる |
| **能動プレイの上振れ** | コンボ・バフの重ねがけ | 放置でも進むが、手を動かすと少し得をする |

## このゲームに足したもの

### 1. 入荷システム(発見)
- スーパーの棚は最初、食パン・牛乳・卵だけ。他の棚は「？？？ 入荷待ち」と表示される。
- 累計の稼ぎが価格の半分に達すると入荷。素材が必要なアップグレードは、その素材が出る色の玉を一度引くまで入荷しない(例: 長いトングは青玉を引くと入荷)。
- 入荷すると「スーパーに新商品入荷: ○○」と通知。画面右に「次の入荷: ??? — 青玉をひく」とヒントを出し、正体は伏せる。
- シミュレーションでは入荷待ちでペースは落ちない(効率のよい買い物順とほぼ一致)ので、演出だけが増える。

### 2. ラッキーごみ(ゴールデンクッキー)
- 1〜3分に1回、ごみ拾い場に金色の缶が出現。15秒で消え、最後の4秒は点滅。
- 拾うとランダムで
  - 50% **大漁タイム**: 30秒間、拾うごみとロボが集めるごみの価値が×7
  - 35% **臨時収入**: 所持金の15%(ロボ15分ぶんの稼ぎが上限、最低300円)
  - 15% **福引券**: ホッパー10回転ぶんの抽選券(最低10枚)
- 放置中(オフライン)には出ない。大漁タイムもオフラインには持ち越さない。

### 3. コンボ(能動プレイの上振れ)
- 2.5秒以内に続けてごみを拾うとコンボが続き、1コンボごとに価値 +1%(最大 +50%)。
- HUD に「23コンボ ×1.22」と表示。効率への影響は小さめ(クリアが約6分早まる程度)で、主に手触りのため。

### 4. スタンプカード(実利のある実績)
- 商店街のスタンプカードとして20個の実績。抽選回数、各色の玉を初めて引く、累計の稼ぎ、ロボ導入、ラッキーごみ、コンボ、クリアなど。
- 1個ごとにごみの買取額が永続で +3%。獲得時に「スタンプ獲得「銀玉をひいた」 買取額 +3% (5/20)」と通知。
- スタンプは周回しても残る。

### 5. 2周目(プレステージ)
- エンディングで [R] を押すと2周目へ。お金・アップグレード・抽選回数はリセットされるが、**思い出ボーナス(買取額×2、3周目は×3…)**、スタンプ、最速記録は持ち越し。
- エンディングに最速記録とスタンプ数を表示。[X] で記録ごと完全に消して最初からも選べる。

### その他
- 画面上部のメッセージを順番待ちにした(入荷・スタンプ・抽選結果が同時に来ても流れて消えない)。
- 大漁タイムの残り秒数を HUD に表示。

## シミュレーション結果(21人の中央値)

| 条件 | 1周目のクリア | 2周目のクリア |
|---|---|---|
| 今回の変更あり | **2時間12分** | **1時間25分** |
| 今回の要素をすべて外す | 4時間16分 | — |
| コンボだけ外す | 2時間17分 | |
| ラッキーごみだけ外す | 2時間40分 | |
| スタンプだけ外す | 2時間43分 | |
| 入荷だけ外す | 2時間12分 | |

1周目で拾ったラッキーごみは中央値21個、獲得スタンプは18/20個。新要素がそのまま効率を上げすぎないよう、コンボを +2%→+1%/回、スタンプを +5%→+3%/個にし、ポイントカードと大型ホッパーの値上がり率を上げて調整しました。

## Unity で確認してほしいこと
1. メニュー `Garapon/Build World` を実行してシーンを作り直す(エンディングに GameData を接続するため)。
2. ラッキーごみが見つけやすいか(大きさ・色・出現間隔)。
3. コンボ表示と大漁タイムの表示が見やすいか。
4. 「？？？ 入荷待ち」の棚が多すぎて寂しくないか。
5. エンディングで [R] を押して2周目に入り、「温泉旅行に行った」スタンプがもらえるか。

## 参考にした資料
- [Numbers Getting Bigger: What Are Incremental Games and Why Are They Fun? (Envato Tuts+)](https://code.tutsplus.com/numbers-getting-bigger-what-are-incremental-games-and-why-are-they-fun--cms-23925a)
- [Numbers Getting Bigger: The Design and Math of Incremental Games (Envato Tuts+)](https://code.tutsplus.com/numbers-getting-bigger-the-design-and-math-of-incremental-games--cms-24023a)
- [Lessons of my first incremental game (Game Developer)](https://www.gamedeveloper.com/design/lessons-of-my-first-incremental-game)
- [Deterding による進行メカニクスの論文(草稿)](https://eprints.whiterose.ac.uk/146788/1/25_Deterding_draft_final_180515_SDE.pdf)
- [Cookie Clicker Wiki(ゴールデンクッキー関連)](https://cookieclicker.wiki.gg/wiki/Dragonflight)
- ['A Dark Room' Is the Most Fun You'll Ever Have with a Spreadsheet (PopMatters)](https://www.popmatters.com/183319-a-dark-room-the-most-fun-youll-ever-have-with-a-spreadsheet-2495645232.html)
- [2017: Universal Paperclips (50 Years of Text Games)](https://if50.substack.com/p/2017-universal-paperclips)
- [Incremental game (Wikipedia)](https://en.wikipedia.org/wiki/Incremental_game)
- [I built 7 idle games in 30 days (DEV Community)](https://dev.to/aguier/i-built-7-idle-games-in-30-days-what-i-learned-about-incremental-design-5d3f)
