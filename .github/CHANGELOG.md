# v1.0.0 - エフェクト除外 for YMM4

YukkuriMovieMaker4向けのエフェクト除外プラグインの初回リリースです。
グループ制御やエフェクトアイテムの効果を、特定のアイテムだけ受けないようにします。
グループ制御の除外はアイテムのエフェクトの構成から対象のエフェクトを取り除くことで行い、エフェクトアイテムの除外は描画の順番を変えることで行います。
除外する相手は、対象の欄に備考欄の文字列を入力して指定します。
8言語のリソース構成のUIを備えます。

---

## 新機能

### 1. 除外パイプライン

`EffectExclusionPipeline`は、Harmony ID `EffectExclusion`でYMM4の描画処理へ実行時パッチを適用します。パッチはエフェクトを最初に生成したときに一度だけ適用します。対象の型やメンバーが見つからない場合には、パッチを適用せず`IsActive`を`false`にして、除外を無効のまま動作を続けます。

| パッチ対象 | 種別 | 役割 |
|---|---|---|
| `EffectedItemSource.UpdateEffects` | prefix | グループ制御に由来するエフェクトをアイテム単位で取り除く |
| `TimelineSource.GetOrderedTimelineResources` | postfix | 除外したアイテムを対象のエフェクトアイテムの直後へ並べ替える |

`FilterParentEffects`は、アイテムのエフェクトの構成をフレームごとに確定する直前に、`ParentEffects`からグループ制御に由来する組を取り除きます。グループ制御が与える映像エフェクトに加えて、座標変換を担う描画エフェクトもあわせて取り除きます。除外はそのフレームから反映され、遅延はありません。

`ReorderTimelineResources`は、描画の順番を並べ替えます。除外の指定を持つアイテムごとに、備考欄が一致するエフェクトアイテムのうち最も後に描画されるものを探し、その直後の位置を表す並べ替えキーを与えます。キーは64ビット整数で、上位に移動先の位置を、下位に元の位置を持たせることで、複数のアイテムを除外した場合にも相対的な描画の順番を維持します。並べ替えが不要なフレームでは、元の並びをそのまま返します。

`EffectedItemSource`、`TimelineSource`、`TimeSourceAndEffectPair`はYMM4の内部の型であるため、リフレクションで参照します。`GroupItem`、`EffectItem`、`IVideoItem`は公開の型であるため、直接参照します。

### 2. エフェクト定義とパラメータ

`EffectExclusionEffect`は、YMM4の映像エフェクトとして宣言されます。

`[VideoEffect]`属性は以下のパラメーターで宣言されます。

- 表示名: `Texts.EffectExclusionEffectName`（ローカライズキー、日本語では「エフェクト除外」）
- カテゴリー: `VideoEffectCategories.Composition`
- 検索タグ: `TagExclusion`・`TagGroupControl`・`TagEffectItem`
- `IsAviUtlSupported = false`によりAviUtl向けEXO出力は非対応
- `ResourceType = typeof(Texts)`でローカライズリソースを指定

公開プロパティは以下のとおりです。

| プロパティ | 型 | デフォルト | アニメーション |
|---|---|---|---|
| `Targets` | `string` | 空文字列 | なし |

`Targets`は複数行のテキストボックスで編集します。`GetAnimatables`は空のシーケンスを返します。`CreateExoVideoFilters`は空のシーケンスを返します（EXO非対応）。エフェクトを最初に生成したときに、更新確認を一度だけ開始します。

映像処理を担う`EffectExclusionEffectProcessor`は、入力の映像を変更せず、`DrawDescription`もそのまま返します。除外の処理はすべて除外パイプラインが行います。

### 3. 対象の一致判定

`Matches`は、備考欄の文字列が除外の対象かどうかを判定します。`Targets`を行ごとに分割し、前後の空白を取り除き、空白だけの行を捨てた一覧と、前後の空白を取り除いた備考欄の文字列を序数比較で照合します。完全に一致した場合にだけ対象になります。

対象の一覧が空の場合には、すべての備考欄が対象になります。一覧は`Targets`を変更したときにだけ作り直し、フレームごとの判定では作り直しません。

### 4. Harmonyの衝突回避

YMM4はプラグインフォルダー内のすべてのDLLを同じ読み込みコンテキストへ読み込むため、複数のプラグインが異なるバージョンの`0Harmony.dll`を同梱すると衝突します。

本プラグインは、Harmony v2.4.2をサブモジュールからソースビルドし、`EffectExclusion.0Harmony.dll`という専用のアセンブリ名で同梱します。アセンブリ名とターゲットフレームワークは、プロジェクト参照の`AdditionalProperties`で注入します。MonoModとMono.CecilはILRepackが`EffectExclusion.0Harmony.dll`の内部へ取り込むため、追加のDLLを必要としません。

### 5. 更新の自動通知

タイムラインでエフェクト除外を最初に使うときに、GitHubの最新のリリースを確認します。現在のバージョンより新しい正式版が公開されている場合には、通知を表示します。確認は1回の起動につき1度だけ行い、プレリリースは通知の対象に含めません。

リリース情報の取得ではmanjuboxのAPIを優先し、応答が得られない場合にはGitHubのAPIへ切り替えます。ネットワークへ接続できないときは、通知を出さずにそのまま動作します。

### 6. ユニットテスト

`EffectExclusion.Tests`は、xunit v3による31件のテストを備えます。

- 対象の一致判定の仕様（空欄、完全一致、空白の除去、複数行、一覧の作り直し）
- YMM4の内部の型とメンバーの存在確認（YMM4の更新による破壊の検知）
- 実際のYMM4アセンブリへのHarmonyパッチの適用確認
- 除外判定と並べ替えキーの計算（一致による移動、相対順の維持、不一致時の無変更）

### 7. ローカライズ

`Texts`クラスは`[AutoGenLocalizer]`属性を持つ`partial`クラスとして宣言されます。
`YukkuriMovieMaker.Generator`のソースジェネレーターが`Texts.csv`を処理し、各ロケールのリソースファイルを自動生成します。

対応リソース: 日本語（`ja-jp`）・英語（`en-us`）・中国語簡体字（`zh-cn`）・中国語繁体字（`zh-tw`）・韓国語（`ko-kr`）・スペイン語（`es-es`）・アラビア語（`ar-sa`）・インドネシア語（`id-id`）

主なローカライズキーは以下のとおりです。

| キー | ja-jp |
|---|---|
| `EffectExclusionEffectName` | エフェクト除外 |
| `Targets` | 対象 |
| `TagExclusion` | 除外 |
| `TagGroupControl` | グループ制御 |
| `TagEffectItem` | エフェクトアイテム |
| `UpdateAvailableMessage` | 新しいバージョン {0} が公開されています。 |
