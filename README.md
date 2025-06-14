# 🌟 P-Deck
# コードはScripts/フォルダ内にあります。

**ポケモンTCGポケット（ポケポケ）対応の非公式カード検索・デッキ構築ツール**  
個人開発で制作したスマートフォン向けアプリです。

---

## 🎮 アプリ概要

- **ジャンル**：カード検索・デッキ構築支援ツール  
- **対応プラットフォーム**：iOS / Android
- **開発期間**：2025年4月10日〜5月15日（約1ヶ月）  
- **開発人数**：1人（個人開発）  
- **開発環境**：Unity 2022.3.6f1 / C#

---

## ✨ 特徴

### ✔ 柔軟なカード検索

- 名前／ひらがな／技テキストで検索可能
- フィルター（進化段階、EX／非EX、エネルギー条件、逃げエネルギーなど）対応

### ✔ 独自のデッキ支援機能

- 山札シャッフルシミュレーション
- デッキメモ機能 

---

## 🛠 技術スタック

| 分類       | 使用技術               |
|------------|------------------------|
| ゲームエンジン | Unity (C#)             |
| データ取得 | GitHub Pages + JSON    |
| UI設計     | UniRx + UniTask        |
| 設計       | MV(R)P構造             |
| 画像       | UnityWebRequestで非同期取得 |

---

## 🧠 実装・設計ポイント

- **MV(R)P構造**：Model/View/Presenterで責務を明確化
- **UniTask採用**：非同期での画像・データ読み込み
- **仮想スクロール**：数百枚のカードでも快適に表示
- **コメント規約**：関数単位で目的を明記

  **→ AIについて**  

> 本プロジェクトでは、AIアシスタント（ChatGPT・Claude）を活用しながら開発を進めました。  
> コード自体はAIが生成した部分もありますが、設計・実装方針・リファクタリング・最終判断は全て自分で行っています。  
> 設計力やAIとの対話による問題解決力を重視した、現代的な開発プロセスを体験・活用しました。


## 📂 ディレクトリ構成（タップで開閉）
<details>
<summary>Assets/Scripts/Cards</summary>

```text
Cards/
├── Model/          # データとビジネスロジック
│   ├── AllCardModel.cs
│   ├── CardDatabase.cs
│   └── CardModel.cs
├── View/           # UI表示とユーザー入力
│   ├── AllCardView.cs
│   └── CardView.cs
├── Presenter/      # ModelとViewの仲介
│   └── AllCardPresenter.cs
└── Utils/          # ユーティリティ
    └── Enum/
        ├── EnumConverter.cs
        └── Enums.cs
```
</details>

<details>
<summary>Assets/Scripts/Deck</summary>

```text
Deck/
├── Model/          # データとビジネスロジック
│   └── DeckModel.cs
├── View/           # UI表示とユーザー入力
│   ├── DeckView.cs
│   ├── DeckViewButton.cs
│   └── SetEnergyPanel.cs
├── Presenter/      # ModelとViewの仲介
│   └── DeckPresenter.cs
├── Manager/        # 管理クラス
│   ├── DeckImageLoader.cs
│   └── DeckManager.cs
└── UI/             # UI専用コンポーネント
    ├── DeckListItem.cs
    ├── DeckListPanel.cs
    └── SampleDeck/
        └── SampleDeckPanel.cs
```
</details>

<details>
<summary>Assets/Scripts/Search</summary>

```text
Search/
├── Model/          # データとビジネスロジック
│   └── SearchModel.cs
├── View/           # UI表示とユーザー入力
│   └── SearchView.cs
├── Presenter/      # ModelとViewの仲介
│   └── SearchPresenter.cs
└── Utils/          # ユーティリティ
    ├── SearchNavigator.cs
    └── Filters/
        ├── Interface/
        │   └── IFilterArea.cs
        ├── CardFilters/
        │   ├── SetCardPackArea.cs
        │   ├── SetCardTypeArea.cs
        │   ├── SetEvolutionStageArea.cs
        │   └── SetTypeArea.cs
        └── NumericFilters/
            ├── SetHPArea.cs
            ├── SetMaxDamageArea.cs
            ├── SetMaxEnergyArea.cs
            └── SetRetreatCostArea.cs
```
</details>

<details>
<summary>Assets/Scripts/CardUIManager</summary>

```text
CardUIManager/
├── Manager/        # 管理クラス
│   └── CardUIManager.cs
├── Presenter/      # ModelとViewの仲介
│   └── CardUIInitializer.cs
├── UI/             # UI専用コンポーネント
│   └── SimpleVirtualScroll.cs
└── Utils/          # ユーティリティ
    ├── CardDataLoader.cs
    └── LazyLoadManager.cs
```
</details>

<details>
<summary>Assets/Scripts/Common</summary>

```text
Common/
├── FeedBack/
│   └── FeedbackContainer.cs
├── Review/
│   └── ReviewManager.cs
└── UI/
    └── TogglePanel.cs
```
</details>

<details>
<summary>Assets/Scripts/その他</summary>

```text
Debug/
└── CacheClearButton.cs

Editor/
└── SampleDeckCreatorWindow.cs

ImageCache/
├── ImageCacheManager.cs
└── ImageDiskCache.cs
```
</details>

## 🚀 リポジトリの構成とビルド可否

> **⚠️ 本リポジトリには *スクリプト (`.cs`)とリファクタリングガイドライン(`.md`)のみ* を収録しています。**  
> 画像・Prefab・Scene などのリソースは **意図的に含めていません**。  
> UI のソフトシャドウには **有料アセット「TrueShadow」** を使用しており、ライセンス上同梱できません。  
> そのため **「クローン → Unity で開く → そのまま Build」** は実行できません。

| シチュエーション | ビルド可否 | 補足 |
|------------------|-----------|------|
| リポジトリをクローン直後 | ❌ **ビルド不可** | 画像 / Prefab / TrueShadowなどが欠落しているため |
| 配布 APK / TestFlight などバイナリ利用 | ✅ **実行のみ可** | 端末にインストールして動作確認が可能 |

### 📦 配布バイナリ

| プラットフォーム | 入手方法 | 備考 |
|------------------|---------|------|
| **iOS** | TestFlight からインストール<br>🔗 <https://testflight.apple.com/join/EQM3bd2Q> | Apple ID でログイン後、参加をタップ |
| **Android** | APK を直接インストール<br>🔗 <https://drive.google.com/drive/folders/1Zr6jSzu8LZTCF0Eaj9QZ0nzhCK8mNAhm?usp=sharing> | “提供元不明アプリ” の許可が必要 |

### なぜ *コードだけ* 公開しているのか

1. **フォルダ構造をシンプルに提示**し、ソースコードレビューをしやすくするため  
2. **ライセンス遵守** — 有料アセットをパブリックリポジトリへ再配布することは規約違反となるため  

---

## ✏️ 著者

**吉田　月輝 / Yoshida Runaki**  
文系学部出身で、2024/3月から独学でUnityを学習しており、UniRxやパフォーマンス最適化にも挑戦しています。  

---

## 📄 使用ライブラリとライセンス

本プロジェクトでは以下のライブラリおよびアセットを使用しています：

| ライブラリ名 | 説明 | ライセンス・備考 |
|--------------|------|------------------|
| [UniRx](https://github.com/neuecc/UniRx) | リアクティブプログラミング | MIT License |
| [UniTask](https://github.com/Cysharp/UniTask) | 非同期処理を効率化するライブラリ | MIT License |
| [DoTween](http://dotween.demigiant.com/) | アニメーション補助ライブラリ | 無料版使用、商用利用可（MIT相当） |
| [TrueShadow](https://assetstore.unity.com/packages/tools/gui/true-shadow-ui-soft-shadow-and-glow-205220) | UIに影を追加する有料アセット | Unity Asset Storeで購入済み、**本リポジトリには含まれていません** |


---

## ⚠ 注意事項

このアプリは非公式の個人開発アプリです。ポケモン公式とは一切関係ありません。---

## ⚖️ 著作権・商標について

- **本アプリはポケモンTCGポケット（Pokémon Trading Card Game Pocket）およびポケモンカードゲームに関連する公式コンテンツとは⼀切関係がありません。**  
- Pokémon、Pokémon Trading Card Game、Nintendo、Creatures Inc.、GAME FREAK inc.、その他関連会社の**登録商標および著作権は各社に帰属**します。  
- 権利者様から指摘・要請があった場合には、**速やかに該当データの削除・⾮公開化など適切な対応**を取ります。

