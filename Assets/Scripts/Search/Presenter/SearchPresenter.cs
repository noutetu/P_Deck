using System.Collections.Generic;
using UnityEngine;

// ----------------------------------------------------------------------
// 検索画面のPresenter
// ViewとModelの橋渡しを行うクラス
// ----------------------------------------------------------------------
public class SearchPresenter
{
    // ----------------------------------------------------------------------
    // Constants - 検索プレゼンター用の定数
    // ----------------------------------------------------------------------
    private static class Constants
    {
        // エラーメッセージ
        public const string ERROR_MODEL_NULL = "SearchModel が null です";
        public const string ERROR_VIEW_NULL = "SearchView が null です";
        public const string ERROR_FILTER_APPLICATION = "フィルター適用中にエラーが発生しました: {0}";
        public const string ERROR_FILTER_CLEAR = "フィルタークリア中にエラーが発生しました: {0}";
        public const string ERROR_BATCH_FILTERING = "バッチフィルタリング中にエラーが発生しました: {0}";
        
        // ログメッセージ
        public const string LOG_FILTER_APPLIED = "フィルターが適用されました";
        public const string LOG_FILTERS_CLEARED = "すべてのフィルターがクリアされました";
        public const string LOG_AREA_REGISTERED = "{0} エリアが登録されました";
        
        // 設定値
        public const int DEFAULT_EMPTY_RESULT_COUNT = 0;
    }

    // ----------------------------------------------------------------------
    // Core Components - コアコンポーネント
    // ----------------------------------------------------------------------
    private SearchView view;
    private SearchModel model;
    
    // ----------------------------------------------------------------------
    // Filter Area References - フィルターエリア参照
    // ----------------------------------------------------------------------
    private SetCardTypeArea cardTypeArea;                       // カードタイプフィルターエリア
    private SetEvolutionStageArea evolutionStageArea;           // 進化段階フィルターエリア
    private SetTypeArea typeArea;                               // ポケモンタイプフィルターエリア
    private SetCardPackArea cardPackArea;                       // カードパックフィルターエリア   
    private SetHPArea hpArea;                                   // HPフィルターエリア
    private SetMaxDamageArea maxDamageArea;                     // 最大ダメージフィルターエリア
    private SetMaxEnergyArea maxEnergyCostArea;                 // 最大エネルギーコストフィルターエリア
    private SetRetreatCostArea retreatCostArea;                 // 逃げるコストフィルターエリア
    
    // ======================================================================
    // Constructor - コンストラクタ
    // ======================================================================
    
    // ----------------------------------------------------------------------
    /// SearchPresenterの初期化
    /// @param view 検索ビューの参照
    /// @param model 検索モデルの参照
    // ----------------------------------------------------------------------
    public SearchPresenter(SearchView view, SearchModel model)
    {
        try
        {
            ValidateComponents(view, model);
            InitializeComponents(view, model);
            SetupEventBindings();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"SearchPresenter initialization failed: {ex.Message}");
            throw;
        }
    }
    
    // ----------------------------------------------------------------------
    /// コンポーネントの妥当性検証
    /// @param view 検索ビューの参照
    /// @param model 検索モデルの参照
    // ----------------------------------------------------------------------
    private void ValidateComponents(SearchView view, SearchModel model)
    {
        if (view == null)
            throw new System.ArgumentNullException(nameof(view), Constants.ERROR_VIEW_NULL);
        if (model == null)
            throw new System.ArgumentNullException(nameof(model), Constants.ERROR_MODEL_NULL);
    }
    
    // ----------------------------------------------------------------------
    /// コンポーネントの初期化
    /// @param view 検索ビューの参照
    /// @param model 検索モデルの参照
    // ----------------------------------------------------------------------
    private void InitializeComponents(SearchView view, SearchModel model)
    {
        this.view = view;
        this.model = model;
    }
    
    // ----------------------------------------------------------------------
    /// イベントバインディングの設定
    // ----------------------------------------------------------------------
    private void SetupEventBindings()
    {
        if (view != null)
        {
            view.OnSearchButtonClicked += ExecuteFilterApplication;
            view.OnClearButtonClicked += ExecuteFilterClear;
        }
    }

    // ======================================================================
    // Filter Area Registration Methods - フィルターエリア登録メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    /// カードタイプエリアの登録
    /// @param area 登録するカードタイプエリア
    // ----------------------------------------------------------------------
    public void RegisterCardTypeArea(SetCardTypeArea area)
    {
        cardTypeArea = area;
        LogAreaRegistration(nameof(SetCardTypeArea));
    }
    
    // ----------------------------------------------------------------------
    /// 進化段階エリアの登録
    /// @param area 登録する進化段階エリア
    // ----------------------------------------------------------------------
    public void RegisterEvolutionStageArea(SetEvolutionStageArea area)
    {
        evolutionStageArea = area;
        LogAreaRegistration(nameof(SetEvolutionStageArea));
    }
    
    // ----------------------------------------------------------------------
    /// ポケモンタイプエリアの登録
    /// @param area 登録するポケモンタイプエリア
    // ----------------------------------------------------------------------
    public void RegisterTypeArea(SetTypeArea area)
    {
        typeArea = area;
        LogAreaRegistration(nameof(SetTypeArea));
    }
    
    // ----------------------------------------------------------------------
    /// カードパックエリアの登録
    /// @param area 登録するカードパックエリア
    // ----------------------------------------------------------------------
    public void RegisterCardPackArea(SetCardPackArea area)
    {
        cardPackArea = area;
        LogAreaRegistration(nameof(SetCardPackArea));
    }
    
    // ----------------------------------------------------------------------
    /// HPエリアの登録
    /// @param area 登録するHPエリア
    // ----------------------------------------------------------------------
    public void RegisterHPArea(SetHPArea area)
    {
        hpArea = area;
        LogAreaRegistration(nameof(SetHPArea));
    }
    
    // ----------------------------------------------------------------------
    /// 最大ダメージエリアの登録
    /// @param area 登録する最大ダメージエリア
    // ----------------------------------------------------------------------
    public void RegisterMaxDamageArea(SetMaxDamageArea area)
    {
        maxDamageArea = area;
        LogAreaRegistration(nameof(SetMaxDamageArea));
    }
    
    // ----------------------------------------------------------------------
    /// 最大エネルギーコストエリアの登録
    /// @param area 登録する最大エネルギーコストエリア
    // ----------------------------------------------------------------------
    public void RegisterMaxEnergyCostArea(SetMaxEnergyArea area)
    {
        maxEnergyCostArea = area;
        LogAreaRegistration(nameof(SetMaxEnergyArea));
    }
    
    // ----------------------------------------------------------------------
    /// 逃げるコストエリアの登録
    /// @param area 登録する逃げるコストエリア
    // ----------------------------------------------------------------------
    public void RegisterRetreatCostArea(SetRetreatCostArea area)
    {
        retreatCostArea = area;
        LogAreaRegistration(nameof(SetRetreatCostArea));
    }
    
    // ----------------------------------------------------------------------
    /// エリア登録ログの出力
    /// @param areaName 登録されたエリア名
    // ----------------------------------------------------------------------
    private void LogAreaRegistration(string areaName)
    {
        Debug.Log(string.Format(Constants.LOG_AREA_REGISTERED, areaName));
    }

    // ======================================================================
    // Filter Application Methods - フィルター適用メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    /// フィルター適用の安全な実行
    // ----------------------------------------------------------------------
    private void ExecuteFilterApplication()
    {
        try
        {
            ApplyAllFiltersToModel();
            Debug.Log(Constants.LOG_FILTER_APPLIED);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_FILTER_APPLICATION, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// すべてのフィルターをモデルに適用
    // ----------------------------------------------------------------------
    private void ApplyAllFiltersToModel()
    {
        if (model == null) return;

        try
        {
            InitializeBatchFiltering();
            ApplyIndividualFilters();
            FinalizeBatchFiltering();
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_BATCH_FILTERING, ex.Message));
            throw;
        }
    }
    
    // ----------------------------------------------------------------------
    /// バッチフィルタリングの初期化
    // ----------------------------------------------------------------------
    private void InitializeBatchFiltering()
    {
        model.BeginBatchFiltering();
    }
    
    // ----------------------------------------------------------------------
    /// 個別フィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyIndividualFilters()
    {
        ApplyFilterIfExists(cardTypeArea, "CardType");
        ApplyFilterIfExists(evolutionStageArea, "EvolutionStage");
        ApplyFilterIfExists(typeArea, "PokemonType");
        ApplyFilterIfExists(cardPackArea, "CardPack");
        ApplyFilterIfExists(hpArea, "HP");
        ApplyFilterIfExists(maxDamageArea, "MaxDamage");
        ApplyFilterIfExists(maxEnergyCostArea, "MaxEnergyCost");
        ApplyFilterIfExists(retreatCostArea, "RetreatCost");
    }
    
    // ----------------------------------------------------------------------
    /// フィルターが存在する場合のみ適用
    /// @param filterArea 適用するフィルターエリア
    /// @param filterName フィルター名（ログ用）
    // ----------------------------------------------------------------------
    private void ApplyFilterIfExists<T>(T filterArea, string filterName) where T : class
    {
        if (filterArea != null)
        {
            try
            {
                // フィルターエリアの型に応じて適用メソッドを呼び出し
                var applyMethod = filterArea.GetType().GetMethod("ApplyFilterToModel");
                applyMethod?.Invoke(filterArea, new object[] { model });
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{filterName} filter application failed: {ex.Message}");
            }
        }
    }
    
    // ----------------------------------------------------------------------
    /// バッチフィルタリングの完了
    // ----------------------------------------------------------------------
    private void FinalizeBatchFiltering()
    {
        model.EndBatchFiltering();
    }

    // ======================================================================
    // Filter Clear Methods - フィルタークリアメソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    /// フィルタークリアの安全な実行
    // ----------------------------------------------------------------------
    private void ExecuteFilterClear()
    {
        try
        {
            ClearAllFilters();
            Debug.Log(Constants.LOG_FILTERS_CLEARED);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_FILTER_CLEAR, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// すべてのフィルターをクリア
    // ----------------------------------------------------------------------
    private void ClearAllFilters()
    {
        ClearFilterAreas();
        ClearModelFilters();
        DisplayEmptyResults();
    }
    
    // ----------------------------------------------------------------------
    /// フィルターエリアのクリア
    // ----------------------------------------------------------------------
    private void ClearFilterAreas()
    {
        ResetFilterIfExists(cardTypeArea, "CardType");
        ResetFilterIfExists(evolutionStageArea, "EvolutionStage");
        ResetFilterIfExists(typeArea, "PokemonType");
        ResetFilterIfExists(cardPackArea, "CardPack");
        ResetFilterIfExists(hpArea, "HP");
        ResetFilterIfExists(maxDamageArea, "MaxDamage");
        ResetFilterIfExists(maxEnergyCostArea, "MaxEnergyCost");
        ResetFilterIfExists(retreatCostArea, "RetreatCost");
    }
    
    // ----------------------------------------------------------------------
    /// フィルターが存在する場合のみリセット
    /// @param filterArea リセットするフィルターエリア
    /// @param filterName フィルター名（ログ用）
    // ----------------------------------------------------------------------
    private void ResetFilterIfExists<T>(T filterArea, string filterName) where T : class
    {
        if (filterArea != null)
        {
            try
            {
                // フィルターエリアの型に応じてリセットメソッドを呼び出し
                var resetMethod = filterArea.GetType().GetMethod("ResetFilters");
                resetMethod?.Invoke(filterArea, null);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{filterName} filter reset failed: {ex.Message}");
            }
        }
    }
    
    // ----------------------------------------------------------------------
    /// モデル側のフィルタークリア
    // ----------------------------------------------------------------------
    private void ClearModelFilters()
    {
        model?.ClearAllFilters();
    }
    
    // ----------------------------------------------------------------------
    /// 空の検索結果を表示
    // ----------------------------------------------------------------------
    private void DisplayEmptyResults()
    {
        if (view != null)
        {
            view.DisplaySearchResults(new List<CardModel>());
        }
    }
}