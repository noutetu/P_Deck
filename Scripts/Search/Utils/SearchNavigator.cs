using System;
using System.Collections.Generic;
using UnityEngine;

// ----------------------------------------------------------------------
// 検索関連のパネル表示/非表示を管理するシングルトンクラス
// パネル間のナビゲーションや検索結果の伝達を担当
// ----------------------------------------------------------------------
public class SearchNavigator : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Constants - 定数管理
    // ----------------------------------------------------------------------
    private static class Constants
    {
        // GameObject関連
        public const string SEARCH_ROUTER_OBJECT_NAME = "SearchRouter";
        
        // ログメッセージ
        public const string LOG_PANELS_SET = "パネル設定完了 - 検索パネル: {0}, カードリストパネル: {1}";
        public const string LOG_SEARCH_PANEL_SHOWN = "検索パネルを表示しました";
        public const string LOG_SEARCH_PANEL_HIDDEN = "検索パネルを非表示にしました";
        public const string LOG_SEARCH_RESULTS_APPLIED = "検索結果を適用しました - 件数: {0}, 購読者数: {1}";
        public const string LOG_SEARCH_CANCELLED = "検索をキャンセルしました";
        public const string LOG_ALL_CARDS_LOADED = "全カードを読み込みました - 件数: {0}";
        
        // エラーメッセージ
        public const string ERROR_PANEL_SETUP = "パネル設定中にエラーが発生しました: {0}";
        public const string ERROR_SEARCH_RESULTS_APPLY = "検索結果の適用中にエラーが発生しました: {0}";
        public const string ERROR_SEARCH_CANCEL = "検索キャンセル処理中にエラーが発生しました: {0}";
        public const string ERROR_ALL_CARDS_LOAD = "全カード読み込み中にエラーが発生しました: {0}";
    }
    
    // ----------------------------------------------------------------------
    // Singleton Pattern Implementation - シングルトンパターンの実装
    // ----------------------------------------------------------------------
    private static SearchNavigator _instance;
    
    public static SearchNavigator Instance
    {
        get
        {
            if (_instance == null)
            {
                CreateSingletonInstance();
            }
            
            return _instance;
        }
    }
    
    // ----------------------------------------------------------------------
    // Panel References - パネル参照
    // ----------------------------------------------------------------------
    [SerializeField] private GameObject searchPanel;         // 検索入力パネル
    [SerializeField] private GameObject cardListPanel;       // カードリストパネル
    
    // ----------------------------------------------------------------------
    // Events - イベント
    // ----------------------------------------------------------------------
    public event Action<List<CardModel>> OnSearchResult;     // 検索結果通知イベント
    
    // ----------------------------------------------------------------------
    // Private Fields - プライベートフィールド
    // ----------------------------------------------------------------------
    private List<CardModel> lastResults = new List<CardModel>();  // 最後に適用された検索結果
    
    // ----------------------------------------------------------------------
    // Singleton Lifecycle Methods - シングルトン生存期間メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief シングルトンインスタンスを作成
    // ----------------------------------------------------------------------
    private static void CreateSingletonInstance()
    {
        try
        {
            GameObject routerObj = new GameObject(Constants.SEARCH_ROUTER_OBJECT_NAME);
            _instance = routerObj.AddComponent<SearchNavigator>();
            DontDestroyOnLoad(routerObj);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"シングルトンインスタンス作成エラー: {ex.Message}");
            throw;
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief Awakeメソッド - シングルトンの初期化
    // ----------------------------------------------------------------------
    private void Awake()
    {
        try
        {
            ValidateSingletonInstance();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Awake処理中にエラー: {ex.Message}");
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief シングルトンインスタンスの妥当性を検証
    // ----------------------------------------------------------------------
    private void ValidateSingletonInstance()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // ----------------------------------------------------------------------
    // Panel Management Methods - パネル管理メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief パネル参照を設定
    /// @param search 検索パネル
    /// @param cardList カードリストパネル
    // ----------------------------------------------------------------------
    public void SetPanels(GameObject search, GameObject cardList)
    {
        try
        {
            ValidateAndAssignPanels(search, cardList);
            InitializePanelStates();
            LogPanelSetup(search, cardList);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_PANEL_SETUP, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief パネル参照の妥当性検証と割り当て
    /// @param search 検索パネル
    /// @param cardList カードリストパネル
    // ----------------------------------------------------------------------
    private void ValidateAndAssignPanels(GameObject search, GameObject cardList)
    {
        searchPanel = search;
        cardListPanel = cardList;
    }
    
    // ----------------------------------------------------------------------
    /// @brief パネルの初期状態を設定
    // ----------------------------------------------------------------------
    private void InitializePanelStates()
    {
        // 初期状態では検索パネルを非表示に
        if (searchPanel) searchPanel.SetActive(false);
        if (cardListPanel && !cardListPanel.activeSelf) cardListPanel.SetActive(true);
    }
    
    // ----------------------------------------------------------------------
    /// @brief パネル設定のログ出力
    /// @param search 検索パネル
    /// @param cardList カードリストパネル
    // ----------------------------------------------------------------------
    private void LogPanelSetup(GameObject search, GameObject cardList)
    {
        string searchStatus = search != null ? "設定済み" : "null";
        string cardListStatus = cardList != null ? "設定済み" : "null";
        Debug.Log(string.Format(Constants.LOG_PANELS_SET, searchStatus, cardListStatus));
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索パネルを表示
    // ----------------------------------------------------------------------
    public void ShowSearchPanel()
    {
        try
        {
            if (CanShowSearchPanel())
            {
                searchPanel.SetActive(true);
                Debug.Log(Constants.LOG_SEARCH_PANEL_SHOWN);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"検索パネル表示エラー: {ex.Message}");
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索パネル表示可能かチェック
    /// @return 表示可能な場合true
    // ----------------------------------------------------------------------
    private bool CanShowSearchPanel()
    {
        return searchPanel != null && !searchPanel.activeSelf;
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索パネルを非表示
    // ----------------------------------------------------------------------
    public void HideSearchPanel()
    {
        try
        {
            if (CanHideSearchPanel())
            {
                searchPanel.SetActive(false);
                Debug.Log(Constants.LOG_SEARCH_PANEL_HIDDEN);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"検索パネル非表示エラー: {ex.Message}");
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索パネル非表示可能かチェック
    /// @return 非表示可能な場合true
    // ----------------------------------------------------------------------
    private bool CanHideSearchPanel()
    {
        return searchPanel != null && searchPanel.activeSelf;
    }
    
    // ----------------------------------------------------------------------
    // Search Results Management Methods - 検索結果管理メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief 検索結果をカードリストに反映
    /// @param results 検索結果のカードリスト
    // ----------------------------------------------------------------------
    public void ApplySearchResults(List<CardModel> results)
    {
        try
        {
            if (IsValidSearchResults(results))
            {
                ProcessSearchResults(results);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_SEARCH_RESULTS_APPLY, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索結果の妥当性を検証
    /// @param results 検索結果
    /// @return 妥当な場合true
    // ----------------------------------------------------------------------
    private bool IsValidSearchResults(List<CardModel> results)
    {
        return results != null;
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索結果を処理
    /// @param results 検索結果
    // ----------------------------------------------------------------------
    private void ProcessSearchResults(List<CardModel> results)
    {
        SaveSearchResults(results);
        NotifySearchResultSubscribers(results);
        LogSearchResultsApplication(results);
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索結果を保存
    /// @param results 検索結果
    // ----------------------------------------------------------------------
    private void SaveSearchResults(List<CardModel> results)
    {
        lastResults = new List<CardModel>(results);
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索結果を購読者に通知
    /// @param results 検索結果
    // ----------------------------------------------------------------------
    private void NotifySearchResultSubscribers(List<CardModel> results)
    {
        OnSearchResult?.Invoke(results);
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索結果適用のログ出力
    /// @param results 検索結果
    // ----------------------------------------------------------------------
    private void LogSearchResultsApplication(List<CardModel> results)
    {
        int subscriberCount = GetSubscriberCount();
        Debug.Log(string.Format(Constants.LOG_SEARCH_RESULTS_APPLIED, results.Count, subscriberCount));
    }
    
    // ----------------------------------------------------------------------
    /// @brief 購読者数を取得
    /// @return 購読者数
    // ----------------------------------------------------------------------
    private int GetSubscriberCount()
    {
        return OnSearchResult?.GetInvocationList().Length ?? 0;
    }
    
    // ----------------------------------------------------------------------
    // Search Cancellation Methods - 検索キャンセルメソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief 検索のキャンセル
    /// パネルを閉じるが、初回のみ全カードを表示
    // ----------------------------------------------------------------------
    public void CancelSearch()
    {
        try
        {
            ExecuteSearchCancellation();
            Debug.Log(Constants.LOG_SEARCH_CANCELLED);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_SEARCH_CANCEL, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索キャンセル処理を実行
    // ----------------------------------------------------------------------
    private void ExecuteSearchCancellation()
    {
        HideSearchPanel();
        
        if (IsFirstTimeCancel())
        {
            LoadAndDisplayAllCards();
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 初回キャンセルかチェック
    /// @return 初回の場合true
    // ----------------------------------------------------------------------
    private bool IsFirstTimeCancel()
    {
        return lastResults.Count == 0;
    }
    
    // ----------------------------------------------------------------------
    /// @brief 全カードを読み込んで表示
    // ----------------------------------------------------------------------
    private void LoadAndDisplayAllCards()
    {
        try
        {
            List<CardModel> allCards = CardDatabase.GetAllCards();
            if (IsValidAllCardsData(allCards))
            {
                ProcessAllCardsDisplay(allCards);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_ALL_CARDS_LOAD, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 全カードデータの妥当性を検証
    /// @param allCards 全カードデータ
    /// @return 妥当な場合true
    // ----------------------------------------------------------------------
    private bool IsValidAllCardsData(List<CardModel> allCards)
    {
        return allCards != null && allCards.Count > 0;
    }
    
    // ----------------------------------------------------------------------
    /// @brief 全カード表示を処理
    /// @param allCards 全カードデータ
    // ----------------------------------------------------------------------
    private void ProcessAllCardsDisplay(List<CardModel> allCards)
    {
        SaveSearchResults(allCards);
        NotifySearchResultSubscribers(allCards);
        LogAllCardsLoaded(allCards);
    }
    
    // ----------------------------------------------------------------------
    /// @brief 全カード読み込みのログ出力
    /// @param allCards 全カードデータ
    // ----------------------------------------------------------------------
    private void LogAllCardsLoaded(List<CardModel> allCards)
    {
        Debug.Log(string.Format(Constants.LOG_ALL_CARDS_LOADED, allCards.Count));
    }
}