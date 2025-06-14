using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
using System;

// ----------------------------------------------------------------------
// ImageDiskCache クラス
// カード画像のディスクキャッシュを管理するクラス。
// 画像をディスクに保存、読み込み、管理する機能を提供します。
// ----------------------------------------------------------------------
public class ImageDiskCache
{
    // ----------------------------------------------------------------------
    // 定数定義クラス
    // ----------------------------------------------------------------------
    public static class Constants
    {
        // デフォルト設定値
        public const string DEFAULT_FOLDER_NAME = "ImageCache";
        public const long DEFAULT_MAX_SIZE_MB = 500;
        public const string METADATA_FILE_NAME = "cache_metadata.json";
        
        // ファイルサイズ変換
        public const long BYTES_PER_MB = 1024 * 1024;
        public const float FLOAT_BYTES_PER_MB = 1024f * 1024f;
        
        // キャッシュ管理
        public const long CACHE_BUFFER_BYTES = 1024 * 1024; // 1MB余裕
        
        // リトライ設定
        public const int MAX_SAVE_RETRIES = 3;
        public const int RETRY_DELAY_BASE_MS = 100;
        
        // ハッシュ形式
        public const string HASH_FORMAT = "x2";
        
        // JSON除外拡張子
        public const string JSON_EXTENSION = ".json";
        
        // テクスチャサイズ
        public const int DEFAULT_TEXTURE_SIZE = 2;
        
        // 日時フォーマット
        public const string DATETIME_FORMAT = "o";
    }

    // ----------------------------------------------------------------------
    // フィールド定義
    // ----------------------------------------------------------------------
    
    // ディスクキャッシュの設定
    private string cacheFolderPath;
    private long maxCacheSize;
    private readonly object cacheLock = new object();

    // キャッシュメタデータ
    private Dictionary<string, CacheMetadata> cacheMetadata = new Dictionary<string, CacheMetadata>();
    private string metadataFilePath;

    // ファイルアクセス用の静的ロックオブジェクト
    private static readonly object fileLock = new object();

    
    // ======================================================================
    // コンストラクタ・初期化メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // コンストラクタ
    // @param folderName キャッシュフォルダ名（デフォルト: "ImageCache"）
    // @param maxSizeMB 最大キャッシュサイズ（MB単位、デフォルト: 500MB）
    // キャッシュフォルダを初期化し、メタデータを読み込みます。
    // ----------------------------------------------------------------------
    public ImageDiskCache(string folderName = Constants.DEFAULT_FOLDER_NAME, long maxSizeMB = Constants.DEFAULT_MAX_SIZE_MB)
    {
        SetupCachePaths(folderName);
        SetupCacheSize(maxSizeMB);
        EnsureCacheDirectoryExists();
        LoadMetadata();
    }
    
    // ----------------------------------------------------------------------
    // キャッシュパスの設定
    // @param folderName キャッシュフォルダ名
    // ----------------------------------------------------------------------
    private void SetupCachePaths(string folderName)
    {
        cacheFolderPath = Path.Combine(Application.persistentDataPath, folderName);
        metadataFilePath = Path.Combine(cacheFolderPath, Constants.METADATA_FILE_NAME);
    }
    
    // ----------------------------------------------------------------------
    // キャッシュサイズの設定
    // @param maxSizeMB 最大キャッシュサイズ（MB単位）
    // ----------------------------------------------------------------------
    private void SetupCacheSize(long maxSizeMB)
    {
        maxCacheSize = maxSizeMB * Constants.BYTES_PER_MB;
    }
    
    // ----------------------------------------------------------------------
    // キャッシュディレクトリの存在確認と作成
    // ----------------------------------------------------------------------
    private void EnsureCacheDirectoryExists()
    {
        if (!Directory.Exists(cacheFolderPath))
        {
            Directory.CreateDirectory(cacheFolderPath);
        }
    }

    // ======================================================================
    // キャッシュ情報取得メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // 現在のキャッシュファイル数を取得
    // @return キャッシュに保存されているファイルの数
    // ----------------------------------------------------------------------
    public int GetFileCount()
    {
        try
        {
            lock (cacheLock)
            {
                return cacheMetadata.Count;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get file count: {ex.Message}");
            return -1;
        }
    }

    
    // ======================================================================
    // ユーティリティメソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // URLからキャッシュキーを生成
    // @param url 画像のURL
    // @return ハッシュ化されたキャッシュキー（MD5を使用）
    // ----------------------------------------------------------------------
    public static string GetKeyFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        return GenerateMD5Hash(url);
    }
    
    // ----------------------------------------------------------------------
    // MD5ハッシュを生成
    // @param input ハッシュ化する文字列
    // @return MD5ハッシュ文字列
    // ----------------------------------------------------------------------
    private static string GenerateMD5Hash(string input)
    {
        using var md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        return ConvertBytesToHexString(hashBytes);
    }
    
    // ----------------------------------------------------------------------
    // バイト配列を16進数文字列に変換
    // @param bytes 変換対象のバイト配列
    // @return 16進数文字列
    // ----------------------------------------------------------------------
    private static string ConvertBytesToHexString(byte[] bytes)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            sb.Append(bytes[i].ToString(Constants.HASH_FORMAT));
        }
        return sb.ToString();
    }

    // ======================================================================
    // 画像保存・読み込みメソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // 画像データをディスクキャッシュに保存
    // @param url 画像のURL
    // @param imageData 画像のバイトデータ
    // @return 保存が成功したかどうか（true: 成功, false: 失敗）
    // キャッシュサイズが最大値を超えないように調整し、画像を保存します。
    // ----------------------------------------------------------------------
    public async UniTask<bool> SaveImageAsync(string url, byte[] imageData)
    {
        try
        {
            if (!ValidateImageData(url, imageData))
            {
                return false;
            }

            string key = GetKeyFromUrl(url);
            string filePath = Path.Combine(cacheFolderPath, key);

            await EnsureSpaceAvailableAsync(imageData.Length);
            await WriteImageToFileAsync(filePath, imageData);
            UpdateMetadataForSavedImage(key, url, imageData);
            await SaveMetadataAsync();

            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save image for URL: {url}, Error: {ex.Message}");
            return false;
        }
    }
    
    // ----------------------------------------------------------------------
    // 画像データの検証
    // @param url 画像のURL
    // @param imageData 画像のバイトデータ
    // @return データが有効かどうか
    // ----------------------------------------------------------------------
    private bool ValidateImageData(string url, byte[] imageData)
    {
        return !string.IsNullOrEmpty(url) && imageData != null && imageData.Length > 0;
    }
    
    // ----------------------------------------------------------------------
    // 画像をファイルに書き込み
    // @param filePath 書き込み先ファイルパス
    // @param imageData 画像のバイトデータ
    // ----------------------------------------------------------------------
    private async UniTask WriteImageToFileAsync(string filePath, byte[] imageData)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await fs.WriteAsync(imageData, 0, imageData.Length);
        }
    }
    
    // ----------------------------------------------------------------------
    // 保存した画像のメタデータを更新
    // @param key キャッシュキー
    // @param url 画像のURL
    // @param imageData 画像のバイトデータ
    // ----------------------------------------------------------------------
    private void UpdateMetadataForSavedImage(string key, string url, byte[] imageData)
    {
        lock (cacheLock)
        {
            cacheMetadata[key] = new CacheMetadata
            {
                Url = url,
                Key = key,
                LastAccessed = DateTime.Now,
                Size = imageData.Length,
                Created = DateTime.Now
            };
        }
    }
    
    
    // ----------------------------------------------------------------------
    // ディスクキャッシュから画像を読み込み
    // @param url 画像のURL
    // @return 画像のバイトデータ（キャッシュになければnull）
    // キャッシュメタデータを更新します。
    // ----------------------------------------------------------------------
    public async UniTask<byte[]> LoadImageAsync(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        
        try
        {
            string key = GetKeyFromUrl(url);
            string filePath = Path.Combine(cacheFolderPath, key);
            
            if (!File.Exists(filePath))
            {
                return null;
            }
            
            byte[] data = await File.ReadAllBytesAsync(filePath);
            UpdateLastAccessTime(key);
            SaveMetadataAsync().Forget();
            
            return data;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load image for URL: {url}, Error: {ex.Message}");
            return null;
        }
    }
    
    // ----------------------------------------------------------------------
    // 最終アクセス時刻を更新
    // @param key キャッシュキー
    // ----------------------------------------------------------------------
    private void UpdateLastAccessTime(string key)
    {
        lock (cacheLock)
        {
            if (cacheMetadata.ContainsKey(key))
            {
                cacheMetadata[key].LastAccessed = DateTime.Now;
            }
        }
    }
    
    // ======================================================================
    // キャッシュ存在確認・削除メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // URLがキャッシュされているかチェック
    // @param url 画像のURL
    // @return キャッシュされているかどうか（true: キャッシュあり, false: キャッシュなし）
    // ----------------------------------------------------------------------
    public bool HasCache(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        
        string key = GetKeyFromUrl(url);
        string filePath = Path.Combine(cacheFolderPath, key);

        lock (cacheLock)
        {
            return File.Exists(filePath) && cacheMetadata.ContainsKey(key);
        }
    }
    
    // ----------------------------------------------------------------------
    // キャッシュを削除
    // @param url 画像のURL
    // @return 削除が成功したかどうか（true: 成功, false: 失敗）
    // ----------------------------------------------------------------------
    public bool RemoveCache(string url)
    {
        if (string.IsNullOrEmpty(url)) return false;
        
        try
        {
            string key = GetKeyFromUrl(url);
            string filePath = Path.Combine(cacheFolderPath, key);
            
            DeleteCacheFile(filePath);
            RemoveFromMetadata(key);
            SaveMetadataAsync().Forget();
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to remove cache for URL: {url}, Error: {ex.Message}");
            return false;
        }
    }
    
    // ----------------------------------------------------------------------
    // キャッシュファイルを削除
    // @param filePath 削除対象のファイルパス
    // ----------------------------------------------------------------------
    private void DeleteCacheFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
    
    // ----------------------------------------------------------------------
    // メタデータから削除
    // @param key 削除対象のキー
    // ----------------------------------------------------------------------
    private void RemoveFromMetadata(string key)
    {
        lock (cacheLock)
        {
            cacheMetadata.Remove(key);
        }
    }
    
    // ----------------------------------------------------------------------
    // すべてのキャッシュを削除
    // @return 削除が成功したかどうか（true: 成功, false: 失敗）
    // キャッシュディレクトリ内のすべてのファイルを削除します。
    // ----------------------------------------------------------------------
    public bool ClearAllCache()
    {
        try
        {
            DeleteAllCacheFiles();
            ClearAllMetadata();
            SaveMetadataAsync().Forget();
            
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to clear all cache: {ex.Message}");
            return false;
        }
    }
    
    // ----------------------------------------------------------------------
    // すべてのキャッシュファイルを削除
    // ----------------------------------------------------------------------
    private void DeleteAllCacheFiles()
    {
        if (Directory.Exists(cacheFolderPath))
        {
            string[] files = Directory.GetFiles(cacheFolderPath);
            foreach (string file in files)
            {
                File.Delete(file);
            }
        }
    }
    
    // ----------------------------------------------------------------------
    // すべてのメタデータをクリア
    // ----------------------------------------------------------------------
    private void ClearAllMetadata()
    {
        lock (cacheLock)
        {
            cacheMetadata.Clear();
        }
    }
    
    
    // ======================================================================
    // キャッシュ容量管理メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // 十分なキャッシュ容量を確保する（古いファイルを削除）
    // @param requiredBytes 必要な容量（バイト単位）
    // キャッシュサイズが上限を超えないように古いファイルを削除します。
    // ----------------------------------------------------------------------
    private async UniTask EnsureSpaceAvailableAsync(long requiredBytes)
    {
        try
        {
            long currentSize = GetCurrentCacheSize();
            
            if (!NeedsSpaceCleanup(currentSize, requiredBytes))
            {
                return;
            }
            
            await PerformSpaceCleanup(currentSize, requiredBytes);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to ensure space available: {ex.Message}");
        }
    }
    
    // ----------------------------------------------------------------------
    // スペースクリーンアップが必要かチェック
    // @param currentSize 現在のキャッシュサイズ
    // @param requiredBytes 必要な容量
    // @return クリーンアップが必要かどうか
    // ----------------------------------------------------------------------
    private bool NeedsSpaceCleanup(long currentSize, long requiredBytes)
    {
        return currentSize + requiredBytes > maxCacheSize;
    }
    
    // ----------------------------------------------------------------------
    // スペースクリーンアップを実行
    // @param currentSize 現在のキャッシュサイズ
    // @param requiredBytes 必要な容量
    // ----------------------------------------------------------------------
    private async UniTask PerformSpaceCleanup(long currentSize, long requiredBytes)
    {
        var sortedItems = GetSortedCacheItems();
        long targetSpace = CalculateTargetSpace(currentSize, requiredBytes);
        
        await DeleteOldCacheItems(sortedItems, targetSpace);
        await SaveMetadataAsync();
    }
    
    // ----------------------------------------------------------------------
    // キャッシュアイテムを最終アクセス順でソート
    // @return ソートされたキャッシュアイテムリスト
    // ----------------------------------------------------------------------
    private List<KeyValuePair<string, CacheMetadata>> GetSortedCacheItems()
    {
        List<KeyValuePair<string, CacheMetadata>> sortedItems;
        lock (cacheLock)
        {
            sortedItems = cacheMetadata.ToList();
        }
        
        sortedItems.Sort((a, b) => a.Value.LastAccessed.CompareTo(b.Value.LastAccessed));
        return sortedItems;
    }
    
    // ----------------------------------------------------------------------
    // 削除すべき容量を計算
    // @param currentSize 現在のキャッシュサイズ
    // @param requiredBytes 必要な容量
    // @return 削除すべき容量
    // ----------------------------------------------------------------------
    private long CalculateTargetSpace(long currentSize, long requiredBytes)
    {
        return currentSize + requiredBytes - maxCacheSize + Constants.CACHE_BUFFER_BYTES;
    }
    
    // ----------------------------------------------------------------------
    // 古いキャッシュアイテムを削除
    // @param sortedItems ソートされたキャッシュアイテム
    // @param targetSpace 削除すべき容量
    // ----------------------------------------------------------------------
    private async UniTask DeleteOldCacheItems(List<KeyValuePair<string, CacheMetadata>> sortedItems, long targetSpace)
    {
        long freedSpace = 0;
        
        foreach (var item in sortedItems)
        {
            if (freedSpace >= targetSpace)
            {
                break;
            }
            
            freedSpace += await DeleteCacheItem(item.Key);
        }
    }
    
    // ----------------------------------------------------------------------
    // 特定のキャッシュアイテムを削除
    // @param key 削除対象のキー
    // @return 削除されたファイルサイズ
    // ----------------------------------------------------------------------
    private UniTask<long> DeleteCacheItem(string key)
    {
        string filePath = Path.Combine(cacheFolderPath, key);
        
        if (!File.Exists(filePath))
        {
            return UniTask.FromResult(0L);
        }
        
        long fileSize = new FileInfo(filePath).Length;
        File.Delete(filePath);
        
        RemoveFromMetadata(key);
        
        return UniTask.FromResult(fileSize);
    }
    
    // ======================================================================
    // キャッシュサイズ計算メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // 現在のキャッシュサイズを取得
    // @return 現在のキャッシュサイズ（バイト単位）
    // キャッシュディレクトリ内のファイルサイズを合計します。
    // ----------------------------------------------------------------------
    private long GetCurrentCacheSize()
    {
        try
        {
            if (!Directory.Exists(cacheFolderPath))
            {
                return 0;
            }
            
            return CalculateTotalFileSize();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get current cache size: {ex.Message}");
            return 0;
        }
    }
    
    // ----------------------------------------------------------------------
    // 総ファイルサイズを計算
    // @return 総ファイルサイズ（バイト単位）
    // ----------------------------------------------------------------------
    private long CalculateTotalFileSize()
    {
        string[] files = Directory.GetFiles(cacheFolderPath);
        long size = 0;
        
        foreach (string file in files)
        {
            if (ShouldExcludeFromSizeCalculation(file))
            {
                continue;
            }
            
            FileInfo fileInfo = new FileInfo(file);
            size += fileInfo.Length;
        }
        
        return size;
    }
    
    // ----------------------------------------------------------------------
    // サイズ計算から除外すべきファイルかチェック
    // @param filePath ファイルパス
    // @return 除外すべきかどうか
    // ----------------------------------------------------------------------
    private bool ShouldExcludeFromSizeCalculation(string filePath)
    {
        return filePath.EndsWith(Constants.JSON_EXTENSION);
    }
    
    // ----------------------------------------------------------------------
    // 現在のディスクキャッシュサイズをMB単位で取得
    // @return 現在のキャッシュサイズ（MB単位）
    // ----------------------------------------------------------------------
    public float GetCacheSizeMB()
    {
        try
        {
            long sizeInBytes = GetCurrentCacheSize();
            return sizeInBytes / Constants.FLOAT_BYTES_PER_MB;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to get cache size in MB: {ex.Message}");
            return 0f;
        }
    }

    
    // ======================================================================
    // メタデータ保存・読み込みメソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // メタデータをJSONに保存
    // キャッシュメタデータをJSON形式で保存します。
    // ----------------------------------------------------------------------
    private async UniTask SaveMetadataAsync()
    {
        try
        {
            EnsureCacheDirectoryExists();
            
            string json = SerializeMetadataToJson();
            await SaveJsonToFileWithRetry(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to save metadata: {ex.Message}");
        }
    }
    
    // ----------------------------------------------------------------------
    // メタデータをJSONにシリアライズ
    // @return JSON文字列
    // ----------------------------------------------------------------------
    private string SerializeMetadataToJson()
    {
        CacheMetadataRoot metadataRoot = new CacheMetadataRoot();
        
        lock (cacheLock)
        {
            metadataRoot.Metadata = new List<CacheMetadata>(cacheMetadata.Values);
        }
        
        return JsonUtility.ToJson(metadataRoot, true);
    }
    
    // ----------------------------------------------------------------------
    // JSONをファイルにリトライ付きで保存
    // @param json 保存するJSON文字列
    // ----------------------------------------------------------------------
    private async UniTask SaveJsonToFileWithRetry(string json)
    {
        bool success = false;
        int retryCount = 0;
        
        while (!success && retryCount < Constants.MAX_SAVE_RETRIES)
        {
            try
            {
                lock (fileLock)
                {
                    File.WriteAllText(metadataFilePath, json);
                }
                success = true;
            }
            catch (IOException ioEx)
            {
                retryCount++;
                Debug.LogWarning($"Metadata save retry {retryCount}: {ioEx.Message}");
                await UniTask.Delay(Constants.RETRY_DELAY_BASE_MS * retryCount);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Critical error saving metadata: {ex.Message}");
                throw;
            }
        }
        
        if (!success)
        {
            Debug.LogError($"Failed to save metadata after {Constants.MAX_SAVE_RETRIES} retries");
        }
    }
    
    // ----------------------------------------------------------------------
    // メタデータをJSONから読み込み
    // キャッシュメタデータをJSON形式から読み込みます。
    // ----------------------------------------------------------------------
    private void LoadMetadata()
    {
        try
        {
            if (!File.Exists(metadataFilePath))
            {
                InitializeEmptyMetadata();
                return;
            }
            
            string json = File.ReadAllText(metadataFilePath);
            
            if (string.IsNullOrWhiteSpace(json))
            {
                InitializeEmptyMetadata();
                return;
            }
            
            DeserializeAndValidateMetadata(json);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to load metadata: {ex.Message}");
            HandleMetadataLoadError();
        }
    }
    
    // ----------------------------------------------------------------------
    // 空のメタデータを初期化
    // ----------------------------------------------------------------------
    private void InitializeEmptyMetadata()
    {
        SaveMetadataAsync().Forget();
    }
    
    // ----------------------------------------------------------------------
    // メタデータをデシリアライズして検証
    // @param json JSON文字列
    // ----------------------------------------------------------------------
    private void DeserializeAndValidateMetadata(string json)
    {
        CacheMetadataRoot metadataRoot = JsonUtility.FromJson<CacheMetadataRoot>(json);
        
        if (metadataRoot?.Metadata != null)
        {
            LoadValidMetadata(metadataRoot.Metadata);
        }
        else
        {
            InitializeEmptyMetadata();
        }
    }
    
    // ----------------------------------------------------------------------
    // 有効なメタデータを読み込み
    // @param metadataList メタデータリスト
    // ----------------------------------------------------------------------
    private void LoadValidMetadata(List<CacheMetadata> metadataList)
    {
        lock (cacheLock)
        {
            cacheMetadata.Clear();
            foreach (var item in metadataList)
            {
                if (ValidateMetadataItem(item))
                {
                    cacheMetadata[item.Key] = item;
                }
            }
        }
    }
    
    // ----------------------------------------------------------------------
    // メタデータアイテムの検証
    // @param item 検証対象のメタデータ
    // @return 有効かどうか
    // ----------------------------------------------------------------------
    private bool ValidateMetadataItem(CacheMetadata item)
    {
        string filePath = Path.Combine(cacheFolderPath, item.Key);
        return File.Exists(filePath);
    }
    
    // ----------------------------------------------------------------------
    // メタデータ読み込みエラーの処理
    // ----------------------------------------------------------------------
    private void HandleMetadataLoadError()
    {
        lock (cacheLock)
        {
            cacheMetadata.Clear();
        }
        
        SaveMetadataAsync().Forget();
    }
    
    // ======================================================================
    // テクスチャ変換メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // テクスチャをバイト配列に変換
    // @param texture 変換対象のテクスチャ
    // @return バイト配列（PNG形式）
    // ----------------------------------------------------------------------
    public static byte[] TextureToBytes(Texture2D texture)
    {
        try
        {
            if (texture == null) return null;
            
            return texture.EncodeToPNG();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to convert texture to bytes: {ex.Message}");
            return null;
        }
    }
    
    // ----------------------------------------------------------------------
    // バイト配列からテクスチャを生成
    // @param bytes バイト配列
    // @return 生成されたテクスチャ（失敗時はnull）
    // ----------------------------------------------------------------------
    public static Texture2D BytesToTexture(byte[] bytes)
    {
        try
        {
            if (bytes == null || bytes.Length == 0) return null;
            
            return CreateTextureFromBytes(bytes);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to convert bytes to texture: {ex.Message}");
            return null;
        }
    }
    
    // ----------------------------------------------------------------------
    // バイト配列からテクスチャを作成
    // @param bytes バイト配列
    // @return 作成されたテクスチャ（失敗時はnull）
    // ----------------------------------------------------------------------
    private static Texture2D CreateTextureFromBytes(byte[] bytes)
    {
        Texture2D texture = new Texture2D(Constants.DEFAULT_TEXTURE_SIZE, Constants.DEFAULT_TEXTURE_SIZE);
        
        if (texture.LoadImage(bytes))
        {
            return texture;
        }
        
        return null;
    }
}

// ----------------------------------------------------------------------
// キャッシュメタデータ構造体
// キャッシュされた画像のメタデータを保持します。
// ----------------------------------------------------------------------
[Serializable]
public class CacheMetadata
{   
    public string Url;              // 画像のURL
    public string Key;              // キャッシュキー（MD5ハッシュ）
    public DateTime LastAccessed;   // 最終アクセス日時
    public DateTime Created;        // 作成日時
    public long Size;               // 画像サイズ（バイト単位）
    
    // Unity JSONシリアライザ用の変換プロパティ
    public string LastAccessedString
    {
        get => LastAccessed.ToString(ImageDiskCache.Constants.DATETIME_FORMAT);
        set => LastAccessed = DateTime.Parse(value);
    }
    
    // Unity JSONシリアライザ用の変換プロパティ
    public string CreatedString
    {
        get => Created.ToString(ImageDiskCache.Constants.DATETIME_FORMAT);
        set => Created = DateTime.Parse(value);
    }
}

// ----------------------------------------------------------------------
// キャッシュメタデータのルートクラス（JSONシリアライズ用）
// メタデータのリストを保持するためのルートクラス。
// ----------------------------------------------------------------------
[Serializable]
public class CacheMetadataRoot
{
    public List<CacheMetadata> Metadata = new List<CacheMetadata>();
}