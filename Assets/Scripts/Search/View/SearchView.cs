using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using TMPro; 

// ----------------------------------------------------------------------
// カード検索画面のView
// ユーザーからの入力を受け取り、検索条件の設定と結果表示を行う
// ----------------------------------------------------------------------
public class SearchView : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Constants - 定数管理
    // ----------------------------------------------------------------------
    private static class Constants
    {
        // UI要素名
        public const string SEARCH_BUTTON_NAME = "Search Button";
        
        // Shader名
        public const string DYNAMIC_GRADIENT_SHADER = "UI/DynamicGradientPro";
        
        // 数値設定
        public const int MIN_VALUE = 0;
        public const int MAX_VALUE = int.MaxValue;
        
        // Vector設定
        public const float SCROLL_POSITION_X = 0f;
        public const float SCROLL_POSITION_Y = 1f;
        
        // 文字列設定
        public const string EMPTY_STRING = "";
        
        // ログメッセージ
        public const string LOG_MVP_SETUP_SUCCESS = "MVP構造のセットアップが完了しました";
        public const string LOG_FILTER_AREA_REGISTERED = "フィルターエリアを登録しました: {0}";
        public const string LOG_SEARCH_EXECUTED = "検索を実行しました - 結果件数: {0}";
        public const string LOG_UI_RESET = "UIをリセットしました";
        public const string LOG_CARDS_DISPLAYED = "検索結果を表示しました - 件数: {0}";
        public const string LOG_SEARCH_PANEL_CLOSED = "検索パネルを閉じました";
        public const string LOG_CARDS_SET = "カードデータを設定しました - 件数: {0}";
        
        // 警告メッセージ
        public const string WARNING_CARDS_NULL = "設定されたカードデータがnullです";
        public const string WARNING_DISPLAY_CARDS_NULL = "表示するカードデータがnullです";
        public const string WARNING_NAVIGATOR_NULL = "SearchNavigator.Instanceが見つかりません";
        public const string WARNING_CARD_PREFAB_NULL = "cardPrefabが設定されていません";
        public const string WARNING_CARD_CONTAINER_NULL = "cardContainerが設定されていません";
        public const string WARNING_CARD_VIEW_NULL = "CardViewコンポーネントが見つかりません: {0}";
        
        // エラーメッセージ
        public const string ERROR_MODEL_NULL = "SearchModelが見つかりません";
        public const string ERROR_MVP_SETUP = "MVP構造のセットアップ中にエラーが発生しました: {0}";
        public const string ERROR_SEARCH_EXECUTION = "検索実行中にエラーが発生しました: {0}";
        public const string ERROR_UI_RESET = "UIリセット中にエラーが発生しました: {0}";
        public const string ERROR_CARD_DISPLAY = "カード表示中にエラーが発生しました: {0}";
        public const string ERROR_LISTENER_SETUP = "リスナー設定中にエラーが発生しました: {0}";
        public const string ERROR_INITIALIZATION = "初期化中にエラーが発生しました: {0}";
        public const string ERROR_UI_INITIALIZATION = "UI初期化中にエラー: {0}";
        public const string ERROR_CARD_DATA_SETUP = "カードデータ設定中にエラーが発生しました: {0}";
        public const string ERROR_PANEL_CLOSE = "検索パネルを閉じる際にエラーが発生しました: {0}";
        public const string ERROR_COMPONENT_DESTROY = "コンポーネント破棄時にエラーが発生しました: {0}";
    }
    
    // ----------------------------------------------------------------------
    // Inspector Settings - Inspector上で設定するコンポーネント
    // ----------------------------------------------------------------------
    [Header("検索入力UI")]
    [SerializeField] private TMP_InputField searchInputField;  // 検索テキスト入力フィールド
    [SerializeField] private Button applyButton;               // OKボタン（決定）
    [SerializeField] private Button cancelButton;              // 閉じるボタン
    [SerializeField] private Button clearButton;               // フィルタリングリセットボタン

    [Header("フィルターエリア")]
    [SerializeField] private SetCardTypeArea cardTypeArea;     // カードタイプフィルターエリア
    [SerializeField] private SetEvolutionStageArea evolutionStageArea; // 進化段階フィルターエリア
    [SerializeField] private SetTypeArea typeArea;             // ポケモンタイプフィルターエリア
    [SerializeField] private SetCardPackArea cardPackArea;     // カードパックフィルターエリア
    [SerializeField] private SetHPArea hpArea;                 // HPフィルターエリア
    [SerializeField] private SetMaxDamageArea maxDamageArea;   // 最大ダメージフィルターエリア
    [SerializeField] private SetMaxEnergyArea maxEnergyCostArea; // 最大エネルギーコストフィルターエリア
    [SerializeField] private SetRetreatCostArea retreatCostArea; // 逃げるコストフィルターエリア

    [Header("結果プレビュー表示UI")]
    [SerializeField] private Transform cardContainer;          // 検索結果表示用コンテナ
    [SerializeField] private GameObject cardPrefab;            // カードプレハブ
    [SerializeField] private ScrollRect scrollRect;            // スクロール領域（オプション）

    // ----------------------------------------------------------------------
    // MVP Pattern Components - MVP管理用
    // ----------------------------------------------------------------------
    private SearchPresenter presenter;
    private SearchModel model;

    // ----------------------------------------------------------------------
    // Events - イベント
    // ----------------------------------------------------------------------
    public event Action OnSearchButtonClicked;
    public event Action OnClearButtonClicked;

    // ----------------------------------------------------------------------
    // Initialization Methods - 初期化メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief 初期化処理
    // ----------------------------------------------------------------------
    private void Start()
    {
        try
        {
            ExecuteInitialization();
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_INITIALIZATION, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 初期化処理を実行
    // ----------------------------------------------------------------------
    private void ExecuteInitialization()
    {
        InitializeUI();
        SetupListeners();
        SetupMVP();
        ResetToInitialState();
    }
    
    // ----------------------------------------------------------------------
    /// @brief 初期状態にリセット
    // ----------------------------------------------------------------------
    private void ResetToInitialState()
    {
        ResetUI();
        ClearModelFilters();
    }
    
    // ----------------------------------------------------------------------
    /// @brief モデルのフィルターをクリア
    // ----------------------------------------------------------------------
    private void ClearModelFilters()
    {
        if (model != null)
        {
            model.ClearAllFilters();
        }
    }    
    // ----------------------------------------------------------------------
    // UI Management Methods - UI管理メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief UI初期化処理
    // ----------------------------------------------------------------------
    private void InitializeUI()
    {
        try
        {
            RefreshUIComponents();
            ResetScrollPosition();
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_UI_INITIALIZATION, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief UIコンポーネントのリフレッシュ
    // ----------------------------------------------------------------------
    private void RefreshUIComponents()
    {
        foreach (var graphic in GetComponentsInChildren<Graphic>())
        {
            RefreshGraphicMaterial(graphic);
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief グラフィックマテリアルのリフレッシュ
    /// @param graphic 対象のGraphicコンポーネント
    // ----------------------------------------------------------------------
    private void RefreshGraphicMaterial(Graphic graphic)
    {
        if (IsValidDynamicGradientMaterial(graphic))
        {
            graphic.SetMaterialDirty();
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief DynamicGradientProマテリアルかチェック
    /// @param graphic 対象のGraphicコンポーネント
    /// @return 有効な場合true
    // ----------------------------------------------------------------------
    private bool IsValidDynamicGradientMaterial(Graphic graphic)
    {
        return graphic.material != null && 
               graphic.material.shader.name == Constants.DYNAMIC_GRADIENT_SHADER;
    }
    
    // ----------------------------------------------------------------------
    /// @brief スクロール位置をリセット
    // ----------------------------------------------------------------------
    private void ResetScrollPosition()
    {
        if (scrollRect != null)
        {
            scrollRect.normalizedPosition = new Vector2(Constants.SCROLL_POSITION_X, Constants.SCROLL_POSITION_Y);
        }
    }

    // ----------------------------------------------------------------------
    // MVP Pattern Setup Methods - MVP設定メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief MVP構造のセットアップ
    // ----------------------------------------------------------------------
    private void SetupMVP()
    {
        try
        {
            InitializeModel();
            InitializePresenter();
            RegisterFilterAreas();
            Debug.Log(Constants.LOG_MVP_SETUP_SUCCESS);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_MVP_SETUP, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief モデルの初期化
    // ----------------------------------------------------------------------
    private void InitializeModel()
    {
        model = SearchModel.Instance;
        if (model == null)
        {
            model = FindFirstObjectByType<SearchModel>();
            if (model == null)
            {
                Debug.LogError(Constants.ERROR_MODEL_NULL);
                return;
            }
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief プレゼンターの初期化
    // ----------------------------------------------------------------------
    private void InitializePresenter()
    {
        if (model != null)
        {
            presenter = new SearchPresenter(this, model);
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief フィルターエリアの登録
    // ----------------------------------------------------------------------
    private void RegisterFilterAreas()
    {
        RegisterCardTypeFilter();
        RegisterEvolutionStageFilter();
        RegisterPokemonTypeFilter();
        RegisterCardPackFilter();
        RegisterNumericFilters();
    }
    
    // ----------------------------------------------------------------------
    /// @brief カードタイプフィルターの登録
    // ----------------------------------------------------------------------
    private void RegisterCardTypeFilter()
    {
        if (cardTypeArea != null && presenter != null)
        {
            presenter.RegisterCardTypeArea(cardTypeArea);
            Debug.Log(string.Format(Constants.LOG_FILTER_AREA_REGISTERED, "カードタイプ"));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 進化段階フィルターの登録
    // ----------------------------------------------------------------------
    private void RegisterEvolutionStageFilter()
    {
        if (evolutionStageArea != null && presenter != null)
        {
            presenter.RegisterEvolutionStageArea(evolutionStageArea);
            Debug.Log(string.Format(Constants.LOG_FILTER_AREA_REGISTERED, "進化段階"));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief ポケモンタイプフィルターの登録
    // ----------------------------------------------------------------------
    private void RegisterPokemonTypeFilter()
    {
        if (typeArea != null && presenter != null)
        {
            presenter.RegisterTypeArea(typeArea);
            Debug.Log(string.Format(Constants.LOG_FILTER_AREA_REGISTERED, "ポケモンタイプ"));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief カードパックフィルターの登録
    // ----------------------------------------------------------------------
    private void RegisterCardPackFilter()
    {
        if (cardPackArea != null && presenter != null)
        {
            presenter.RegisterCardPackArea(cardPackArea);
            Debug.Log(string.Format(Constants.LOG_FILTER_AREA_REGISTERED, "カードパック"));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 数値フィルターの登録
    // ----------------------------------------------------------------------
    private void RegisterNumericFilters()
    {
        RegisterHPFilter();
        RegisterMaxDamageFilter();
        RegisterMaxEnergyFilter();
        RegisterRetreatCostFilter();
    }
    
    // ----------------------------------------------------------------------
    /// @brief HPフィルターの登録
    // ----------------------------------------------------------------------
    private void RegisterHPFilter()
    {
        if (hpArea != null && presenter != null)
        {
            presenter.RegisterHPArea(hpArea);
            Debug.Log(string.Format(Constants.LOG_FILTER_AREA_REGISTERED, "HP"));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 最大ダメージフィルターの登録
    // ----------------------------------------------------------------------
    private void RegisterMaxDamageFilter()
    {
        if (maxDamageArea != null && presenter != null)
        {
            presenter.RegisterMaxDamageArea(maxDamageArea);
            Debug.Log(string.Format(Constants.LOG_FILTER_AREA_REGISTERED, "最大ダメージ"));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 最大エネルギーフィルターの登録
    // ----------------------------------------------------------------------
    private void RegisterMaxEnergyFilter()
    {
        if (maxEnergyCostArea != null && presenter != null)
        {
            presenter.RegisterMaxEnergyCostArea(maxEnergyCostArea);
            Debug.Log(string.Format(Constants.LOG_FILTER_AREA_REGISTERED, "最大エネルギー"));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 逃げるコストフィルターの登録
    // ----------------------------------------------------------------------
    private void RegisterRetreatCostFilter()
    {
        if (retreatCostArea != null && presenter != null)
        {
            presenter.RegisterRetreatCostArea(retreatCostArea);
            Debug.Log(string.Format(Constants.LOG_FILTER_AREA_REGISTERED, "逃げるコスト"));
        }
    }

    // ----------------------------------------------------------------------
    // External Data Management Methods - 外部データ管理メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief カードデータを外部から設定（エラー対策）
    /// @param cards 設定するカードデータのリスト
    // ----------------------------------------------------------------------
    public void SetCards(List<CardModel> cards)
    {
        try
        {
            if (cards == null)
            {
                Debug.LogWarning(Constants.WARNING_CARDS_NULL);
                return;
            }

            if (model == null)
            {
                SetupMVP();
            }

            if (model != null)
            {
                model.SetCards(cards);
                Debug.Log(string.Format(Constants.LOG_CARDS_SET, cards.Count));
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_CARD_DATA_SETUP, ex.Message));
        }
    }

    // ----------------------------------------------------------------------
    // Event Listener Setup Methods - イベントリスナー設定メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief リスナーの設定
    // ----------------------------------------------------------------------
    private void SetupListeners()
    {
        try
        {
            SetupSearchInputListeners();
            SetupButtonListeners();
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_LISTENER_SETUP, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索入力フィールドのリスナー設定
    // ----------------------------------------------------------------------
    private void SetupSearchInputListeners()
    {
        if (searchInputField == null) return;

        SetupTextChangeListener();
        SetupEndEditListener();
        SetupSearchButtonListener();
    }
    
    // ----------------------------------------------------------------------
    /// @brief テキスト変更リスナーの設定
    // ----------------------------------------------------------------------
    private void SetupTextChangeListener()
    {
        searchInputField.onValueChanged.AddListener((text) =>
        {
            if (model != null)
            {
                model.SetSearchText(text);
            }
        });
    }
    
    // ----------------------------------------------------------------------
    /// @brief 入力完了リスナーの設定
    // ----------------------------------------------------------------------
    private void SetupEndEditListener()
    {
        searchInputField.onEndEdit.AddListener((text) =>
        {
            if (model != null)
            {
                model.ExecuteSearchAndFilters();
            }
        });
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索ボタンリスナーの設定
    // ----------------------------------------------------------------------
    private void SetupSearchButtonListener()
    {
        var searchIcon = searchInputField.transform.Find(Constants.SEARCH_BUTTON_NAME);
        if (searchIcon != null && searchIcon.GetComponent<Button>() != null)
        {
            var button = searchIcon.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                if (model != null)
                {
                    model.ExecuteSearchAndFilters();
                }
            });
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief ボタンリスナーの設定
    // ----------------------------------------------------------------------
    private void SetupButtonListeners()
    {
        SetupApplyButtonListener();
        SetupClearButtonListener();
        SetupCancelButtonListener();
    }
    
    // ----------------------------------------------------------------------
    /// @brief OKボタンリスナーの設定
    // ----------------------------------------------------------------------
    private void SetupApplyButtonListener()
    {
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(() =>
            {
                OnSearchButtonClicked?.Invoke();
                ApplySearchResults();
            });
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief クリアボタンリスナーの設定
    // ----------------------------------------------------------------------
    private void SetupClearButtonListener()
    {
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(() =>
            {
                OnClearButtonClicked?.Invoke();
                ResetUI();
            });
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 閉じるボタンリスナーの設定
    // ----------------------------------------------------------------------
    private void SetupCancelButtonListener()
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(CloseSearchPanel);
        }
    }

    // ----------------------------------------------------------------------
    // Search Result Application Methods - 検索結果適用メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief 検索結果を適用して検索パネルを閉じる
    // ----------------------------------------------------------------------
    public void ApplySearchResults()
    {
        try
        {
            if (SearchNavigator.Instance == null)
            {
                Debug.LogWarning(Constants.WARNING_NAVIGATOR_NULL);
                return;
            }

            var filterData = CollectFilterData();
            var results = ExecuteSearchWithFilters(filterData);
            
            SearchNavigator.Instance.ApplySearchResults(results);
            CloseSearchPanel();
            
            Debug.Log(string.Format(Constants.LOG_SEARCH_EXECUTED, results?.Count ?? 0));
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_SEARCH_EXECUTION, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief フィルターデータを収集
    /// @return 収集されたフィルターデータ
    // ----------------------------------------------------------------------
    private SearchFilterData CollectFilterData()
    {
        return new SearchFilterData
        {
            CardTypes = cardTypeArea?.GetSelectedCardTypes().ToList() ?? new List<Enum.CardType>(),
            EvolutionStages = evolutionStageArea?.GetSelectedEvolutionStages().ToList() ?? new List<Enum.EvolutionStage>(),
            PokemonTypes = typeArea?.GetSelectedTypes().ToList() ?? new List<Enum.PokemonType>(),
            CardPacks = cardPackArea?.GetSelectedCardPacks().ToList() ?? new List<Enum.CardPack>(),
            HPRange = CalculateHPRange(),
            DamageRange = CalculateDamageRange(),
            EnergyCostRange = CalculateEnergyCostRange(),
            RetreatCostRange = CalculateRetreatCostRange()
        };
    }
    
    // ----------------------------------------------------------------------
    /// @brief HP範囲を計算
    /// @return HP範囲（min, max）
    // ----------------------------------------------------------------------
    private (int min, int max) CalculateHPRange()
    {
        if (hpArea == null) return (Constants.MIN_VALUE, Constants.MAX_VALUE);

        var hpVal = hpArea.GetSelectedHP();
        var cmp = hpArea.GetSelectedComparisonType();
        
        return cmp switch
        {
            SetHPArea.HPComparisonType.LessOrEqual => (Constants.MIN_VALUE, hpVal),
            SetHPArea.HPComparisonType.Equal => (hpVal, hpVal),
            SetHPArea.HPComparisonType.GreaterOrEqual => (hpVal, Constants.MAX_VALUE),
            _ => (Constants.MIN_VALUE, Constants.MAX_VALUE)
        };
    }
    
    // ----------------------------------------------------------------------
    /// @brief ダメージ範囲を計算
    /// @return ダメージ範囲（min, max）
    // ----------------------------------------------------------------------
    private (int min, int max) CalculateDamageRange()
    {
        if (maxDamageArea == null) return (Constants.MIN_VALUE, Constants.MAX_VALUE);

        var dmg = maxDamageArea.GetSelectedDamage();
        var cmp = maxDamageArea.GetSelectedComparisonType();
        
        return cmp switch
        {
            SetMaxDamageArea.DamageComparisonType.LessOrEqual => (Constants.MIN_VALUE, dmg),
            SetMaxDamageArea.DamageComparisonType.Equal => (dmg, dmg),
            SetMaxDamageArea.DamageComparisonType.GreaterOrEqual => (dmg, Constants.MAX_VALUE),
            _ => (Constants.MIN_VALUE, Constants.MAX_VALUE)
        };
    }
    
    // ----------------------------------------------------------------------
    /// @brief エネルギーコスト範囲を計算
    /// @return エネルギーコスト範囲（min, max）
    // ----------------------------------------------------------------------
    private (int min, int max) CalculateEnergyCostRange()
    {
        if (maxEnergyCostArea == null) return (Constants.MIN_VALUE, Constants.MAX_VALUE);

        var cost = maxEnergyCostArea.GetSelectedEnergyCost();
        var cmp = maxEnergyCostArea.GetSelectedComparisonType();
        
        return cmp switch
        {
            SetMaxEnergyArea.EnergyComparisonType.LessOrEqual => (Constants.MIN_VALUE, cost),
            SetMaxEnergyArea.EnergyComparisonType.Equal => (cost, cost),
            SetMaxEnergyArea.EnergyComparisonType.GreaterOrEqual => (cost, Constants.MAX_VALUE),
            _ => (Constants.MIN_VALUE, Constants.MAX_VALUE)
        };
    }
    
    // ----------------------------------------------------------------------
    /// @brief 逃げるコスト範囲を計算
    /// @return 逃げるコスト範囲（min, max）
    // ----------------------------------------------------------------------
    private (int min, int max) CalculateRetreatCostRange()
    {
        if (retreatCostArea == null) return (Constants.MIN_VALUE, Constants.MAX_VALUE);

        var cost = retreatCostArea.GetSelectedRetreatCost();
        var cmp = retreatCostArea.GetSelectedComparisonType();
        
        return cmp switch
        {
            SetRetreatCostArea.RetreatComparisonType.LessOrEqual => (Constants.MIN_VALUE, cost),
            SetRetreatCostArea.RetreatComparisonType.Equal => (cost, cost),
            SetRetreatCostArea.RetreatComparisonType.GreaterOrEqual => (cost, Constants.MAX_VALUE),
            _ => (Constants.MIN_VALUE, Constants.MAX_VALUE)
        };
    }
    
    // ----------------------------------------------------------------------
    /// @brief フィルターデータで検索を実行
    /// @param filterData フィルターデータ
    /// @return 検索結果のカードリスト
    // ----------------------------------------------------------------------
    private List<CardModel> ExecuteSearchWithFilters(SearchFilterData filterData)
    {
        if (model == null) return new List<CardModel>();

        return model.Search(
            filterData.CardTypes,
            filterData.EvolutionStages,
            filterData.PokemonTypes,
            filterData.CardPacks,
            filterData.HPRange.min, filterData.HPRange.max,
            filterData.DamageRange.min, filterData.DamageRange.max,
            filterData.EnergyCostRange.min, filterData.EnergyCostRange.max,
            filterData.RetreatCostRange.min, filterData.RetreatCostRange.max
        );
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索フィルターデータ構造体
    // ----------------------------------------------------------------------
    private struct SearchFilterData
    {
        public List<Enum.CardType> CardTypes;
        public List<Enum.EvolutionStage> EvolutionStages;
        public List<Enum.PokemonType> PokemonTypes;
        public List<Enum.CardPack> CardPacks;
        public (int min, int max) HPRange;
        public (int min, int max) DamageRange;
        public (int min, int max) EnergyCostRange;
        public (int min, int max) RetreatCostRange;
    }

    // ----------------------------------------------------------------------
    // Panel Management Methods - パネル管理メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief 検索パネルを閉じる
    // ----------------------------------------------------------------------
    private void CloseSearchPanel()
    {
        try
        {
            if (SearchNavigator.Instance != null)
            {
                SearchNavigator.Instance.CancelSearch();
                Debug.Log(Constants.LOG_SEARCH_PANEL_CLOSED);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_PANEL_CLOSE, ex.Message));
        }
    }

    // ----------------------------------------------------------------------
    // UI Reset Methods - UIリセットメソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief UI要素のリセット
    // ----------------------------------------------------------------------
    public void ResetUI()
    {
        try
        {
            ClearCardContainer();
            ClearSearchInput();
            ResetAllFilters();
            Debug.Log(Constants.LOG_UI_RESET);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_UI_RESET, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索入力フィールドをクリア
    // ----------------------------------------------------------------------
    private void ClearSearchInput()
    {
        if (searchInputField != null)
        {
            searchInputField.text = Constants.EMPTY_STRING;
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 全フィルターをリセット
    // ----------------------------------------------------------------------
    private void ResetAllFilters()
    {
        ResetBasicFilters();
        ResetNumericFilters();
    }
    
    // ----------------------------------------------------------------------
    /// @brief 基本フィルターをリセット
    // ----------------------------------------------------------------------
    private void ResetBasicFilters()
    {
        cardTypeArea?.ResetFilters();
        evolutionStageArea?.ResetFilters();
        typeArea?.ResetFilters();
        cardPackArea?.ResetFilters();
    }
    
    // ----------------------------------------------------------------------
    /// @brief 数値フィルターをリセット
    // ----------------------------------------------------------------------
    private void ResetNumericFilters()
    {
        hpArea?.ResetFilters();
        maxDamageArea?.ResetFilters();
        maxEnergyCostArea?.ResetFilters();
        retreatCostArea?.ResetFilters();
    }

    // ----------------------------------------------------------------------
    /// @brief カードコンテナをクリア
    // ----------------------------------------------------------------------
    private void ClearCardContainer()
    {
        if (cardContainer == null) return;

        foreach (Transform child in cardContainer)
        {
            Destroy(child.gameObject);
        }
    }
    
    // ----------------------------------------------------------------------
    // Card Display Methods - カード表示メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief 検索結果の表示
    /// @param cards 表示するカードデータのリスト
    // ----------------------------------------------------------------------
    public void DisplaySearchResults(List<CardModel> cards)
    {
        try
        {
            if (cards == null)
            {
                Debug.LogWarning(Constants.WARNING_DISPLAY_CARDS_NULL);
                return;
            }

            ClearCardContainer();
            
            if (!ValidateCardDisplayComponents())
            {
                return;
            }

            DisplayCards(cards);
            ResetScrollPosition();
            
            Debug.Log(string.Format(Constants.LOG_CARDS_DISPLAYED, cards.Count));
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_CARD_DISPLAY, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief カード表示コンポーネントの検証
    /// @return 有効な場合true
    // ----------------------------------------------------------------------
    private bool ValidateCardDisplayComponents()
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning(Constants.WARNING_CARD_PREFAB_NULL);
            return false;
        }
        
        if (cardContainer == null)
        {
            Debug.LogWarning(Constants.WARNING_CARD_CONTAINER_NULL);
            return false;
        }
        
        return true;
    }
    
    // ----------------------------------------------------------------------
    /// @brief カードを表示
    /// @param cards 表示するカードリスト
    // ----------------------------------------------------------------------
    private void DisplayCards(List<CardModel> cards)
    {
        foreach (var card in cards)
        {
            CreateAndConfigureCard(card);
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief カードを作成・設定
    /// @param card カードデータ
    // ----------------------------------------------------------------------
    private void CreateAndConfigureCard(CardModel card)
    {
        GameObject cardObj = Instantiate(cardPrefab, cardContainer);
        CardView cardView = cardObj.GetComponent<CardView>();

        if (cardView != null)
        {
            cardView.SetImage(card);
        }
        else
        {
            Debug.LogWarning(string.Format(Constants.WARNING_CARD_VIEW_NULL, card.name));
        }
    }

    // ----------------------------------------------------------------------
    // Lifecycle Management Methods - ライフサイクル管理メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// @brief コンポーネント破棄時の処理
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        try
        {
            RemoveSearchInputListeners();
            RemoveButtonListeners();
        }
        catch (System.Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_COMPONENT_DESTROY, ex.Message));
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索入力リスナーの削除
    // ----------------------------------------------------------------------
    private void RemoveSearchInputListeners()
    {
        if (searchInputField == null) return;

        searchInputField.onEndEdit.RemoveAllListeners();
        searchInputField.onValueChanged.RemoveAllListeners();
        
        RemoveSearchButtonListener();
    }
    
    // ----------------------------------------------------------------------
    /// @brief 検索ボタンリスナーの削除
    // ----------------------------------------------------------------------
    private void RemoveSearchButtonListener()
    {
        var searchIcon = searchInputField.transform.Find(Constants.SEARCH_BUTTON_NAME);
        if (searchIcon != null && searchIcon.GetComponent<Button>() != null)
        {
            searchIcon.GetComponent<Button>().onClick.RemoveAllListeners();
        }
    }
    
    // ----------------------------------------------------------------------
    /// @brief ボタンリスナーの削除
    // ----------------------------------------------------------------------
    private void RemoveButtonListeners()
    {
        clearButton?.onClick.RemoveAllListeners();
        applyButton?.onClick.RemoveAllListeners();
        cancelButton?.onClick.RemoveAllListeners();
    }
}