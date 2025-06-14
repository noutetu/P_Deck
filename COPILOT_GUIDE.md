# P-Deck プロジェクト コーディングガイド

## プロジェクト概要
このドキュメントは、P-Deckプロジェクト（Unity C#）におけるコーディング規約とベストプラクティスを定義します。
GitHub Copilotが一貫性のあるコードを生成できるよう、具体的な例とチェックリストを提供します。

## ファイル構造とメンバー順序
Unityクラスファイル内のメンバーは以下の順序で配置してください：

1. **定数 (Constants)**
2. **フィールド (Fields)** - SerializeFieldを含む
3. **コンストラクタ (Constructor)**
4. **Unityライフサイクルメソッド** - `Awake()`, `Start()`, `Update()` など
5. **初期化メソッド (Initialization)**
6. **公開メソッド (Public API)**
7. **主要機能メソッド (Core Functionality)**
8. **ヘルパーメソッド (Helper Methods)**
9. **イベントハンドラー (Event Handlers)**
10. **ユーティリティメソッド (Utility)**
11. **クリーンアップメソッド (Cleanup)**

## 命名規則

### 基本ルール
- **クラス**: PascalCase、名詞を使用
- **メソッド**: PascalCase（アクセス修飾子に関係なく）、動詞+目的語の形式
- **変数**: camelCase、意味のある名詞
- **定数**: ALL_CAPS_WITH_UNDERSCORES

### 例
```csharp
// ✅ 良い例
public class ImageDiskCache
{
    private const string DEFAULT_CACHE_FOLDER = "ImageCache";
    private string cacheFolderPath;
    
    public void SetupCachePaths() { }
    private void ValidateCacheDirectory() { }
}

// ❌ 悪い例
public class diskcache
{
    private string tmp;
    private const string folderName = "cache";
    
    public void func1() { }
    private void validatecache() { }
}
```

---

## コード品質とメソッド設計

### 単一責任の原則 (SRP)
- 各メソッドは1つの明確な責任のみを持つ
- メソッドの長さは30行以下を推奨
- サイクロマティック複雑度は10以下を目標
- 複雑になったら小さなヘルパーメソッドに分割

```csharp
// ✅ 良い例 - 責任が分離されている
public async Task SaveImageAsync(string url, byte[] imageData)
{
    ValidateImageData(imageData);
    await WriteImageToFileAsync(url, imageData);
}

private void ValidateImageData(byte[] data)
{
    if (data == null || data.Length == 0)
        throw new ArgumentException("画像データが無効です");
}

private async Task WriteImageToFileAsync(string url, byte[] data)
{
    var path = GetCacheFilePath(url);
    await File.WriteAllBytesAsync(path, data);
}

// ❌ 悪い例 - 1つのメソッドが複数の責任を持つ
public async Task SaveImageAsync(string url, byte[] imageData)
{
    // 200行の巨大なメソッド
    // 検証、変換、保存、ログ出力など複数の処理が混在
}
```

### 未使用コードの除去
定義されているが使用されていないメソッド、プロパティ、変数は削除してください。

```csharp
// ✅ 良い例 - 使用されているメソッドのみ残す
private void UpdateCardCount() 
{ 
    cardCount = cards.Count; 
}

// ❌ 悪い例 - 未使用メソッドは削除する
private void UpdateNameCount() { /* どこからも呼ばれていない */ }
```

### DRY原則 (Don't Repeat Yourself)
重複するコードは共通メソッドに抽出してください。

```csharp
// ✅ 良い例 - 共通メソッドを作成
private bool IsSameCardType(Card a, Card b) => a.Type == b.Type;

// ❌ 悪い例 - 同じロジックを重複して定義
private bool IsSameCardTypeA(Card a, Card b) => a.Type == b.Type;
private bool IsSameCardTypeB(Card a, Card b) => a.Type == b.Type;
```

---
## コメント記述規則

メソッドの説明コメントは以下の形式で記述してください：

```csharp
// ----------------------------------------------------------------------
// キャッシュに画像を保存します
// @param url 画像のURL
// @param imageData 画像のバイト配列データ
// @return 保存に成功した場合はtrue、失敗した場合はfalse
// ----------------------------------------------------------------------
public async Task<bool> SaveImageToCacheAsync(string url, byte[] imageData)
{
    // 実装
}
```

## var使用ガイドライン

### var使用を推奨するケース
- LINQクエリの結果で右辺から型が明白な場合
- 匿名型やTupleなど型名が冗長になる場合
- foreachループの反復変数

### var使用を避けるケース
- 型が不明瞭になる場合

```csharp
// ✅ 良い例 - 型が明確
var users = dbContext.Users.Where(u => u.IsActive);
var count = cards.Count;
foreach (var item in cardCollection) { }

// ❌ 悪い例 - 型が不明瞭
var result = DoSomethingMystery(); // 戻り値の型が分からない
var data = GetData(); // 何のデータか不明
```

---
## Unity固有のルール

### SerializeField
プライベートフィールドを Inspector で表示する場合は `[SerializeField]` を使用し、publicは避けてください。

```csharp
// ✅ 良い例
[SerializeField] private Texture2D cardIcon;
[SerializeField] private Button actionButton;

// ❌ 悪い例
public Texture2D cardIcon; // カプセル化が破綻
```

### Update()メソッドの最適化
- 入力検知など軽量な処理のみ
- 重い処理は早期returnで回避
- 毎フレーム実行される重いLINQクエリは避ける

```csharp
// ✅ 良い例
private void Update()
{
    if (!isGameActive) return; // 早期return
    
    if (Input.GetKeyDown(KeyCode.Space))
    {
        HandleSpaceKeyPress();
    }
}

// ❌ 悪い例
private void Update()
{
    var expensiveQuery = cards.Where(c => c.IsSpecial).ToList(); // 毎フレーム重い処理
}
```

### オブジェクトプール
頻繁にオブジェクトを生成/破棄する場合はオブジェクトプールを使用してください。

```csharp
// ✅ 良い例
var bullet = bulletPool.Get();
// 使用後
bulletPool.Release(bullet);

// ❌ 悪い例
var bullet = Instantiate(bulletPrefab); // 毎回生成
Destroy(bullet); // 毎回破棄
```

---

## 例外処理とエラーハンドリング

### 基本原則
- 公開メソッドは適切な例外処理を実装
- 具体的な例外タイプを投げる
- エラーメッセージは定数で管理

```csharp
// 定数でエラーメッセージを管理
public static class ErrorMessages
{
    public const string ERROR_METADATA_SAVE = "メタデータの保存に失敗しました";
    public const string ERROR_INVALID_CARD_DATA = "無効なカードデータです";
}

// 例外処理の実装例
public async Task SaveMetadataAsync(CardMetadata metadata)
{
    try
    {
        ValidateMetadata(metadata);
        await WriteMetadataToFileAsync(metadata);
    }
    catch (IOException ex)
    {
        Debug.LogError(ErrorMessages.ERROR_METADATA_SAVE, this);
        throw new InvalidOperationException(ErrorMessages.ERROR_METADATA_SAVE, ex);
    }
    catch (ArgumentException ex)
    {
        Debug.LogError(ErrorMessages.ERROR_INVALID_CARD_DATA, this);
        throw;
    }
}
```

## マジックナンバーの定数化

ハードコードされた数値は定数として定義してください。

```csharp
// ✅ 良い例
public static class GameConstants
{
    public const int MAX_SAVE_RETRIES = 3;
    public const int DEFAULT_CARD_COUNT = 60;
    public const float ANIMATION_DURATION = 0.5f;
}

if (retryCount > GameConstants.MAX_SAVE_RETRIES)
{
    return false;
}

// ❌ 悪い例
if (retryCount > 3) // マジックナンバー
{
    return false;
}
```

---
## コード品質チェックリスト

コード作成・レビュー時は以下の項目を確認してください：

### 📋 基本チェック項目
- [ ] **未使用コードの削除**: 使用されていないメソッド、プロパティ、変数を削除したか
- [ ] **マジックナンバーの定数化**: ハードコードされた数値を適切な定数に置き換えたか
- [ ] **メソッドの分割**: 30行を超えるメソッドを適切に分割したか
- [ ] **命名規則の遵守**: クラス、メソッド、変数の命名がガイドラインに沿っているか
- [ ] **SRP (単一責任の原則)**: 各メソッドが単一の明確な責任を持っているか

### 🎮 Unity固有チェック項目
- [ ] **SerializeFieldの適切な使用**: publicフィールドではなく`[SerializeField]`を使用しているか
- [ ] **Update()最適化**: Update()内に重い処理が含まれていないか
- [ ] **オブジェクトプール**: 頻繁な生成/破棄でパフォーマンス問題がないか
- [ ] **Debug.Logの管理**: 本番環境で不要なログ出力がないか

### 🔍 コード品質チェック項目
- [ ] **varの適切な使用**: 型が不明瞭になるvarの使用がないか
- [ ] **例外処理**: 公開メソッドに適切な例外処理が実装されているか
- [ ] **DRY原則**: 重複するコードが共通メソッドに抽出されているか
- [ ] **コメント**: 複雑なロジックに適切な説明コメントが付いているか

---

## まとめ

このガイドラインに従うことで、保守性が高く、一貫性のあるコードベースを維持できます。
GitHub Copilotを使用する際も、これらの規約に沿った提案を期待できるようになります。

質問や追加の規約が必要な場合は、チーム内で議論して本ドキュメントを更新してください。
