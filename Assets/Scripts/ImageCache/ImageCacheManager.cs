using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using Cysharp.Threading.Tasks;
using System;

// ----------------------------------------------------------------------
// カード画像の読み込みとキャッシュを管理するクラス
// メモリキャッシュとディスクキャッシュの階層的管理を行い、
// UniTaskを使用した非同期読み込みと効率的なキャッシング機能を提供
// ----------------------------------------------------------------------
public class ImageCacheManager : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // 定数定義クラス
    // ----------------------------------------------------------------------
    private static class Constants
    {
        // デフォルト設定値
        public const int DEFAULT_MAX_CACHE_SIZE_MB = 500;
        public const bool DEFAULT_USE_MEMORY_CACHE = true;
        public const bool DEFAULT_USE_DISK_CACHE = true;
        
        // キャッシュディレクトリ名
        public const string CACHE_DIRECTORY_NAME = "ImageCache";
        
        // フィードバックメッセージ
        public const string FEEDBACK_INITIALIZING = "画像キャッシュシステムを初期化中...";
        public const string FEEDBACK_INITIALIZATION_COMPLETE = "画像キャッシュシステムの初期化が完了しました";
        public const float FEEDBACK_COMPLETION_DELAY = 0.5f;
        
        // 待機時間
        public const int DUPLICATE_LOADING_YIELD_INTERVAL = 1;
    }

    // ----------------------------------------------------------------------
    // シングルトンインスタンス
    // ----------------------------------------------------------------------
    private static ImageCacheManager _instance;
    public static ImageCacheManager Instance
    {
        get
        {
            return _instance;
        }
    }

    // ----------------------------------------------------------------------
    // フィールド定義
    // ----------------------------------------------------------------------
    
    // キャッシュ関連
    private Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
    private ImageDiskCache diskCache;
    private HashSet<string> loadingUrls = new HashSet<string>();
    
    // デフォルトテクスチャ
    [SerializeField] private Texture2D defaultTexture;
    private Texture2D _defaultTexture;
    
    // 設定項目
    [SerializeField] private int maxCacheSizeMB = Constants.DEFAULT_MAX_CACHE_SIZE_MB;
    [SerializeField] private bool useMemoryCache = Constants.DEFAULT_USE_MEMORY_CACHE;
    [SerializeField] private bool useDiskCache = Constants.DEFAULT_USE_DISK_CACHE;
    
    // ======================================================================
    // Unity ライフサイクルメソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // UnityのAwakeメソッド
    // シングルトンパターンを実装し、初期化処理を実行
    // ----------------------------------------------------------------------
    private void Awake()
    {
        if (!SetupSingleton())
        {
            return;
        }
        
        SetupDefaultTexture();
        ShowInitializationFeedback();
        InitializeDiskCache();
        CompleteInitializationFeedback();
    }
    
    // ======================================================================
    // 初期化関連メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // シングルトンパターンのセットアップ
    // @returns 初期化を続行するかどうか
    // ----------------------------------------------------------------------
    private bool SetupSingleton()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            return true;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return false;
        }
        return true;
    }
    
    // ----------------------------------------------------------------------
    // デフォルトテクスチャのセットアップ
    // ----------------------------------------------------------------------
    private void SetupDefaultTexture()
    {
        _defaultTexture = defaultTexture;
    }
    
    // ----------------------------------------------------------------------
    // 初期化開始フィードバックの表示
    // ----------------------------------------------------------------------
    private void ShowInitializationFeedback()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowProgressFeedback(Constants.FEEDBACK_INITIALIZING);
        }
    }
    
    // ----------------------------------------------------------------------
    // ディスクキャッシュの初期化
    // ----------------------------------------------------------------------
    private void InitializeDiskCache()
    {
        if (useDiskCache)
        {
            diskCache = new ImageDiskCache(Constants.CACHE_DIRECTORY_NAME, maxCacheSizeMB);
        }
    }
    
    // ----------------------------------------------------------------------
    // 初期化完了フィードバックの表示
    // ----------------------------------------------------------------------
    private void CompleteInitializationFeedback()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.CompleteProgressFeedback(
                Constants.FEEDBACK_INITIALIZATION_COMPLETE, 
                Constants.FEEDBACK_COMPLETION_DELAY
            );
        }
    }
    
    // ======================================================================
    // テクスチャ読み込み関連メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // URLからテクスチャを読み込み、キャッシュする
    // @param url 画像のURL
    // @param assignToCard カード画像を設定する対象のCardModel（オプション）
    // @returns 読み込んだテクスチャ
    // ----------------------------------------------------------------------
    public async UniTask<Texture2D> LoadTextureAsync(string url, CardModel assignToCard = null)
    {
        try
        {
            // URL検証
            if (!ValidateUrl(url, assignToCard))
            {
                return _defaultTexture;
            }

            // 重複読み込み制御
            if (await HandleDuplicateLoading(url, assignToCard))
            {
                return GetCachedTextureOrDefault(url, assignToCard);
            }

            // 読み込み開始
            loadingUrls.Add(url);

            // キャッシュ階層での検索・読み込み
            var texture = await LoadTextureFromCacheHierarchy(url, assignToCard);
            
            return texture;
        }
        catch (Exception ex)
        {
            Debug.LogError($"LoadTextureAsync failed for URL: {url}, Error: {ex.Message}");
            return HandleLoadingError(url, assignToCard);
        }
    }
    
    // ----------------------------------------------------------------------
    // URL検証処理
    // @param url 検証するURL
    // @param assignToCard カードモデル（オプション）
    // @returns URLが有効かどうか
    // ----------------------------------------------------------------------
    private bool ValidateUrl(string url, CardModel assignToCard)
    {
        if (string.IsNullOrEmpty(url))
        {
            AssignTextureToCard(assignToCard, _defaultTexture);
            return false;
        }
        return true;
    }
    
    // ----------------------------------------------------------------------
    // 重複読み込み制御処理
    // @param url 読み込み対象のURL
    // @param assignToCard カードモデル（オプション）
    // @returns 重複読み込みかどうか
    // ----------------------------------------------------------------------
    private async UniTask<bool> HandleDuplicateLoading(string url, CardModel assignToCard)
    {
        if (!loadingUrls.Contains(url))
        {
            return false;
        }

        // 読み込み完了を待機
        await WaitForLoadingCompletion(url);
        return true;
    }
    
    // ----------------------------------------------------------------------
    // 読み込み完了待機処理
    // @param url 待機対象のURL
    // ----------------------------------------------------------------------
    private async UniTask WaitForLoadingCompletion(string url)
    {
        while (loadingUrls.Contains(url))
        {
            await UniTask.Yield();
        }
    }
    
    // ----------------------------------------------------------------------
    // キャッシュ階層からのテクスチャ読み込み処理
    // @param url 読み込み対象のURL
    // @param assignToCard カードモデル（オプション）
    // @returns 読み込んだテクスチャ
    // ----------------------------------------------------------------------
    private async UniTask<Texture2D> LoadTextureFromCacheHierarchy(string url, CardModel assignToCard)
    {
        Texture2D texture = null;

        // 1. メモリキャッシュから検索
        texture = LoadFromMemoryCache(url);
        if (texture != null)
        {
            return CompleteTextureLoading(url, texture, assignToCard);
        }

        // 2. ディスクキャッシュから検索
        texture = await LoadFromDiskCache(url);
        if (texture != null)
        {
            SaveToMemoryCache(url, texture);
            return CompleteTextureLoading(url, texture, assignToCard);
        }

        // 3. ネットワークからダウンロード
        texture = await DownloadTextureFromNetwork(url);
        if (texture != null)
        {
            await SaveToCaches(url, texture);
            return CompleteTextureLoading(url, texture, assignToCard);
        }

        return CompleteTextureLoading(url, _defaultTexture, assignToCard);
    }
    
    // ----------------------------------------------------------------------
    // メモリキャッシュからテクスチャを読み込み
    // @param url 読み込み対象のURL
    // @returns キャッシュされたテクスチャ（存在しない場合はnull）
    // ----------------------------------------------------------------------
    private Texture2D LoadFromMemoryCache(string url)
    {
        if (useMemoryCache && textureCache.TryGetValue(url, out Texture2D texture))
        {
            return texture;
        }
        return null;
    }
    
    // ----------------------------------------------------------------------
    // ディスクキャッシュからテクスチャを読み込み
    // @param url 読み込み対象のURL
    // @returns ディスクキャッシュからのテクスチャ（存在しない場合はnull）
    // ----------------------------------------------------------------------
    private async UniTask<Texture2D> LoadFromDiskCache(string url)
    {
        if (!useDiskCache || diskCache == null)
        {
            return null;
        }

        byte[] imageData = await diskCache.LoadImageAsync(url);
        if (imageData != null)
        {
            return ImageDiskCache.BytesToTexture(imageData);
        }
        return null;
    }
    
    // ----------------------------------------------------------------------
    // ネットワークからテクスチャをダウンロード
    // @param url ダウンロード対象のURL
    // @returns ダウンロードしたテクスチャ（失敗時はnull）
    // ----------------------------------------------------------------------
    private async UniTask<Texture2D> DownloadTextureFromNetwork(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Failed to download texture from: {url}, Error: {request.error}");
                return null;
            }

            return DownloadHandlerTexture.GetContent(request);
        }
    }
    
    // ----------------------------------------------------------------------
    // テクスチャをキャッシュに保存
    // @param url 保存対象のURL
    // @param texture 保存するテクスチャ
    // ----------------------------------------------------------------------
    private async UniTask SaveToCaches(string url, Texture2D texture)
    {
        // ディスクキャッシュに保存
        await SaveToDiskCache(url, texture);
        
        // メモリキャッシュに保存
        SaveToMemoryCache(url, texture);
    }
    
    // ----------------------------------------------------------------------
    // ディスクキャッシュにテクスチャを保存
    // @param url 保存対象のURL
    // @param texture 保存するテクスチャ
    // ----------------------------------------------------------------------
    private async UniTask SaveToDiskCache(string url, Texture2D texture)
    {
        if (useDiskCache && diskCache != null && texture != null)
        {
            byte[] textureBytes = ImageDiskCache.TextureToBytes(texture);
            if (textureBytes != null)
            {
                await diskCache.SaveImageAsync(url, textureBytes);
            }
        }
    }
    
    // ----------------------------------------------------------------------
    // メモリキャッシュにテクスチャを保存
    // @param url 保存対象のURL
    // @param texture 保存するテクスチャ
    // ----------------------------------------------------------------------
    private void SaveToMemoryCache(string url, Texture2D texture)
    {
        if (useMemoryCache && texture != null)
        {
            textureCache[url] = texture;
        }
    }
    
    // ----------------------------------------------------------------------
    // テクスチャ読み込み完了処理
    // @param url 読み込み対象のURL
    // @param texture 読み込んだテクスチャ
    // @param assignToCard カードモデル（オプション）
    // @returns 読み込んだテクスチャ
    // ----------------------------------------------------------------------
    private Texture2D CompleteTextureLoading(string url, Texture2D texture, CardModel assignToCard)
    {
        loadingUrls.Remove(url);
        AssignTextureToCard(assignToCard, texture);
        return texture;
    }
    
    // ----------------------------------------------------------------------
    // カードにテクスチャを割り当て
    // @param assignToCard 割り当て対象のカードモデル
    // @param texture 割り当てるテクスチャ
    // ----------------------------------------------------------------------
    private void AssignTextureToCard(CardModel assignToCard, Texture2D texture)
    {
        if (assignToCard != null)
        {
            assignToCard.imageTexture = texture;
        }
    }
    
    // ----------------------------------------------------------------------
    // キャッシュされたテクスチャを取得またはデフォルトを返す
    // @param url 取得対象のURL
    // @param assignToCard カードモデル（オプション）
    // @returns キャッシュされたテクスチャまたはデフォルト
    // ----------------------------------------------------------------------
    private Texture2D GetCachedTextureOrDefault(string url, CardModel assignToCard)
    {
        if (textureCache.TryGetValue(url, out Texture2D cachedTexture))
        {
            AssignTextureToCard(assignToCard, cachedTexture);
            return cachedTexture;
        }
        
        AssignTextureToCard(assignToCard, _defaultTexture);
        return _defaultTexture;
    }
    
    // ----------------------------------------------------------------------
    // 読み込みエラー処理
    // @param url エラーが発生したURL
    // @param assignToCard カードモデル（オプション）
    // @returns デフォルトテクスチャ
    // ----------------------------------------------------------------------
    private Texture2D HandleLoadingError(string url, CardModel assignToCard)
    {
        try
        {
            loadingUrls.Remove(url);
        }
        catch (Exception cleanupEx)
        {
            Debug.LogError($"Error during cleanup for URL: {url}, Error: {cleanupEx.Message}");
        }

        AssignTextureToCard(assignToCard, _defaultTexture);
        return _defaultTexture;
    }
    
    
    // ======================================================================
    // カード専用メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // カードのテクスチャを取得または読み込む（UniTask版）
    // @param card 対象のカードモデル
    // @returns カードのテクスチャ
    // ----------------------------------------------------------------------
    public async UniTask<Texture2D> GetCardTextureAsync(CardModel card)
    {
        if (card == null)
            return _defaultTexture;
            
        // すでにテクスチャが設定されている場合はそれを返す
        if (card.imageTexture != null)
            return card.imageTexture;
            
        // URLが空の場合はデフォルトを返す
        if (string.IsNullOrEmpty(card.imageKey))
        {
            card.imageTexture = _defaultTexture;
            return _defaultTexture;
        }
        
        // 画像を読み込んでカードに設定
        var texture = await LoadTextureAsync(card.imageKey, card);
        return texture;
    }
    
    // ----------------------------------------------------------------------
    // カードのテクスチャがキャッシュされているかチェック
    // @param card 対象のカードモデル
    // @returns キャッシュされているかどうか
    // ----------------------------------------------------------------------
    public bool IsCardTextureCached(CardModel card)
    {
        if (card == null || string.IsNullOrEmpty(card.imageKey))
            return false;
            
        return IsTextureCached(card.imageKey);
    }
    
    // ----------------------------------------------------------------------
    // カードのキャッシュ済みテクスチャを取得（同期版）
    // @param card 対象のカードモデル
    // @returns キャッシュされたテクスチャ（存在しない場合はデフォルト）
    // ----------------------------------------------------------------------
    public Texture2D GetCachedCardTexture(CardModel card)
    {
        if (card == null || string.IsNullOrEmpty(card.imageKey))
            return _defaultTexture;
            
        return GetCachedTexture(card.imageKey);
    }
    
    // ======================================================================
    // キャッシュ管理メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // キャッシュをクリア（メモリとディスク両方）
    // ----------------------------------------------------------------------
    public void ClearAllCache()
    {
        ClearMemoryCache();
        ClearDiskCacheIfEnabled();
    }

    // ----------------------------------------------------------------------
    // メモリキャッシュのみクリア
    // ----------------------------------------------------------------------
    public void ClearMemoryCache()
    {
        textureCache.Clear();
    }
    
    // ----------------------------------------------------------------------
    // ディスクキャッシュをクリア（有効な場合のみ）
    // ----------------------------------------------------------------------
    private void ClearDiskCacheIfEnabled()
    {
        if (useDiskCache && diskCache != null)
        {
            diskCache.ClearAllCache();
        }
    }

    // ----------------------------------------------------------------------
    // 特定のURLのキャッシュを削除
    // @param url 削除対象のURL
    // ----------------------------------------------------------------------
    public void RemoveCache(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        RemoveFromMemoryCache(url);
        RemoveFromDiskCache(url);
    }
    
    // ----------------------------------------------------------------------
    // メモリキャッシュから特定のURLを削除
    // @param url 削除対象のURL
    // ----------------------------------------------------------------------
    private void RemoveFromMemoryCache(string url)
    {
        if (textureCache.ContainsKey(url))
        {
            textureCache.Remove(url);
        }
    }
    
    // ----------------------------------------------------------------------
    // ディスクキャッシュから特定のURLを削除
    // @param url 削除対象のURL
    // ----------------------------------------------------------------------
    private void RemoveFromDiskCache(string url)
    {
        if (useDiskCache && diskCache != null)
        {
            diskCache.RemoveCache(url);
        }
    }
    
    // ======================================================================
    // テクスチャ取得・検索メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // 指定されたURLのテクスチャがキャッシュされているかチェック
    // @param url 確認対象のURL
    // @returns キャッシュされているかどうか
    // ----------------------------------------------------------------------
    public bool IsTextureCached(string url)
    {
        if (string.IsNullOrEmpty(url))
            return false;
            
        return useMemoryCache && textureCache.ContainsKey(url);
    }
    
    // ----------------------------------------------------------------------
    // キャッシュからテクスチャを取得（同期版）
    // キャッシュにない場合はnullを返す
    // @param url 取得対象のURL
    // @returns キャッシュされたテクスチャ（存在しない場合はnull）
    // ----------------------------------------------------------------------
    public Texture2D GetCachedTexture(string url)
    {
        if (string.IsNullOrEmpty(url))
            return _defaultTexture;
            
        if (useMemoryCache && textureCache.TryGetValue(url, out Texture2D texture))
        {
            return texture;
        }
        
        return null;
    }
    
    // ======================================================================
    // デフォルトテクスチャ管理メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // デフォルトテクスチャを取得
    // @returns デフォルトテクスチャ
    // ----------------------------------------------------------------------
    public Texture2D GetDefaultTexture()
    {
        return _defaultTexture;
    }
    
    // ----------------------------------------------------------------------
    // デフォルトテクスチャを設定（外部から設定可能）
    // @param texture 設定するテクスチャ
    // ----------------------------------------------------------------------
    public void SetDefaultTexture(Texture2D texture)
    {
        if (texture != null)
        {
            _defaultTexture = texture;
        }
    }
}