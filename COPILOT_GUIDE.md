# COPILOT_GUIDE.md

> P-Deck プロジェクト用 **GitHub Copilot 学習ガイド（日本語）**  
> *200 行以内・見出しは ### まで*

---

### 目的
- Copilot に **望ましい書き方**・**避けたい書き方** を学習させ、修正コストを下げる。
- *チェックリスト + 良い／悪いペア例* に特化し、詳細な理論解説は省く。

---

### ファイル内メンバー並び順
1. **Constants / 定数**
2. **Fields / フィールド**
3. **Constructor / コンストラクタ**
4. **Lifecycle Methods / ライフサイクル** (`Awake`, `Start`, `Update` …)
5. **Initialization / 初期化メソッド**
6. **Public API / 公開メソッド**
7. **Core Functionality / 主要機能**
8. **Helper Methods / ヘルパーメソッド**
9. **Event Handlers / イベントハンドラー**
10. **Utility / ユーティリティ**
11. **Cleanup / クリーンアップ**

---

### 命名規則
| 種別 | 規則 | 良い例 | 悪い例 |
|------|------|--------|--------|
| クラス | **PascalCase** / 名詞 | `ImageDiskCache` | `diskcache` |
| メソッド | public: **PascalCase** / private: **camelCase** / 動詞+目的語 | `SetupCachePaths()` | `func1()` |
| 変数 | **camelCase** / 意味のある名詞 | `cacheFolderPath` | `tmp` |
| 定数 | **ALL_CAPS + _** | `DEFAULT_FOLDER_NAME` | `folderName` |

---

### コードレイアウト & SRP
- 1 メソッドは **30 行以下** または **サイクロマティック複雑度 10 以下**。
- 複雑化したらヘルパーに分割。メソッドは単一責任の原則（SRP）を遵守し、一つのことだけを行うように設計してください。
- コメントを記述する際、または既存のコメントを確認する際は、そのメソッドがSRPに従い、かつ30行以内であることを確認してください。もしそうでなければ、メソッドの分割を優先的に検討してください。
  **良**: `SaveImageAsync()` → `ValidateImageData()` + `WriteImageToFileAsync()`  
  **悪**: 200 行の `SaveImageAsync()` が巨大化。

---

### 未使用要素の徹底除去
```csharp
// ✅ 良い
private void UpdateNameCount() { /* ... */ }

// ❌ 悪い（呼ばれていない）
private void UpdateNameCount() { /* ... */ }
DRY（重複コード排除）
csharp
コピーする
// ✅ 良い
bool IsSameCardType(Card a, Card b) => a.Type == b.Type;

// ❌ 悪い
bool IsSameCardTypeA(Card a, Card b) => a.Type == b.Type;
bool IsSameCardTypeB(Card a, Card b) => a.Type == b.Type;
コメント書式
csharp
コピーする
// ----------------------------------------------------------------------
// キャッシュを保存
// @param url        画像 URL
// @param imageData  バイト配列
// @return 保存成功なら true
// ----------------------------------------------------------------------
// メソッドの説明コメントは、上記の例のように `// ---...` の枠線内に記述してください。
var の使用ガイド
型が不明瞭になる場合は var を使わない。

次のケースは var を許可する。

LINQ クエリ結果で右辺から型が明白

匿名型 / Tuple など型名が冗長

foreach (var item in collection) のループ変数

csharp
コピーする
// ✅ 良い
var users = dbContext.Users.Where(u => u.IsActive);

// ❌ 悪い（型が読めない）
var result = DoSomethingMystery();
Unity 専用ルール
項目    良い例    悪い例
SerializeField    private [SerializeField] Texture2D icon;    public Texture2D icon;
Update() 最適化    入力検知のみ & 早期 return    毎フレーム重い LINQ
オブジェクトプール    Get() / Release()    Instantiate / Destroy 連打

例外・エラーハンドリング
公開メソッドは try–catch で包み、具体例外を投げる。

エラーメッセージは Constants に統一。

csharp
コピーする
try { await SaveMetadataAsync(); }
catch (IOException ex) { Debug.LogError(Constants.ERROR_METADATA_SAVE, this); }
ハードコード値の定数化
csharp
コピーする
// ✅ 良い
if (retryCount > Constants.MAX_SAVE_RETRIES) return;

// ❌ 悪い
if (retryCount > 3) return;
チェックリスト（保存前に確認）
 未使用コードを削除したか

 マジックナンバーを定数化したか

 30 行超メソッドを分割したか

 命名規則に沿っているか

 Debug.Log は本番で無駄に出ないか

 var の乱用がないか

 Unity の Update() 内に重い処理がないか
