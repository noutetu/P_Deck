using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Enum;
using System.Text; // ひらがな・カタカナ変換用
using TMPro;  // TMP_InputField用
using UnityEngine.UI; // Buttonコンポーネント用

// ----------------------------------------------------------------------
// カード検索のモデルクラス
// 検索条件の管理とフィルタリング処理を担当する
// ----------------------------------------------------------------------
public class SearchModel : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // Constants - 検索システムで使用する定数
    // ----------------------------------------------------------------------
    public static class Constants
    {
        // 検索設定
        public const float SEARCH_DELAY_SECONDS = 0.3f;
        public const int MIN_SEARCH_LENGTH = 2;
        
        // デフォルト値
        public const int DEFAULT_HP_MIN = 30;
        public const int DEFAULT_HP_MAX = 200;
        public const int DEFAULT_DAMAGE_MIN = 0;
        public const int DEFAULT_DAMAGE_MAX = 200;
        public const int DEFAULT_ENERGY_COST_MIN = 0;
        public const int DEFAULT_ENERGY_COST_MAX = 5;
        public const int DEFAULT_RETREAT_COST_MIN = 0;
        public const int DEFAULT_RETREAT_COST_MAX = 4;
        
        // フィードバックメッセージ
        public const string SEARCH_RESULT_FORMAT = " 検索結果: {0}件";
        public const float FEEDBACK_DISPLAY_DURATION = 0.8f;
        
        // ヒラガナ・カタカナ変換
        public const char KATAKANA_START = '\u30A1';
        public const char KATAKANA_END = '\u30F6';
        public const int KATAKANA_TO_HIRAGANA_OFFSET = 0x60;
        
        // UI要素
        public const string SEARCH_BUTTON_NAME = "Search Button";
    }

    // シングルトンインスタンス
    public static SearchModel Instance { get; private set; }

    // ----------------------------------------------------------------------
    // 検索入力フィールド
    // ----------------------------------------------------------------------
    [SerializeField] private TMP_InputField searchInputField;

    // ----------------------------------------------------------------------
    // 検索遅延制御
    // ----------------------------------------------------------------------
    private float currentSearchDelayTimer = 0f;
    private bool searchIsDue = false;
    private string lastExecutedSearchText = "";

    // ----------------------------------------------------------------------
    // カードデータ
    // ----------------------------------------------------------------------
    private List<CardModel> allCards = new List<CardModel>();
    private List<CardModel> filteredCards = new List<CardModel>();
    private List<CardModel> cardList = null;

    // ----------------------------------------------------------------------
    // 検索テキストとフィルター状態
    // ----------------------------------------------------------------------
    private string searchText = "";
    private bool isBatchFiltering = false;

    // ----------------------------------------------------------------------
    // フィルター状態 - カードタイプ、進化段階、ポケモンタイプ、カードパック
    // ----------------------------------------------------------------------
    private HashSet<CardType> selectedCardTypes = new HashSet<CardType>();
    private HashSet<EvolutionStage> selectedEvolutionStages = new HashSet<EvolutionStage>();
    private HashSet<PokemonType> selectedPokemonTypes = new HashSet<PokemonType>();
    private HashSet<CardPack> selectedCardPacks = new HashSet<CardPack>();

    // ----------------------------------------------------------------------
    // フィルター状態 - 数値フィルター（HP、最大ダメージ、エネルギーコスト、逃げるコスト）
    // ----------------------------------------------------------------------
    private int selectedHP = 0;
    private SetHPArea.HPComparisonType selectedHPComparisonType = SetHPArea.HPComparisonType.None;
    
    private int selectedMaxDamage = 0;
    private SetMaxDamageArea.DamageComparisonType selectedMaxDamageComparisonType = SetMaxDamageArea.DamageComparisonType.None;
    
    private int selectedMaxEnergyCost = 0;
    private SetMaxEnergyArea.EnergyComparisonType selectedMaxEnergyCostComparisonType = SetMaxEnergyArea.EnergyComparisonType.None;
    
    private int selectedRetreatCost = 0;
    private SetRetreatCostArea.RetreatComparisonType selectedRetreatCostComparisonType = SetRetreatCostArea.RetreatComparisonType.None;

    // ----------------------------------------------------------------------
    // ライフサイクル
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// コンポーネント初期化
    // ----------------------------------------------------------------------
    private void Awake()
    {
        InitializeSingleton();
    }

    // ----------------------------------------------------------------------
    /// 開始時処理
    // ----------------------------------------------------------------------
    private void Start()
    {
        SetupSearchInputField();
        Initialize();
    }

    // ----------------------------------------------------------------------
    /// フレーム更新処理（検索遅延制御）
    // ----------------------------------------------------------------------
    private void Update()
    {
        ProcessDelayedSearch();
    }

    // ----------------------------------------------------------------------
    // 初期化
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// シングルトンパターンの初期化
    // ----------------------------------------------------------------------
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ----------------------------------------------------------------------
    /// 遅延検索の処理
    // ----------------------------------------------------------------------
    private void ProcessDelayedSearch()
    {
        if (!searchIsDue) return;

        currentSearchDelayTimer -= Time.deltaTime;
        if (currentSearchDelayTimer <= 0f)
        {
            ExecuteDelayedSearch();
        }
    }

    // ----------------------------------------------------------------------
    /// 遅延検索の実行（重複防止機能付き）
    // ----------------------------------------------------------------------
    private void ExecuteDelayedSearch()
    {
        if (searchText != lastExecutedSearchText)
        {
            ExecuteSearchAndFilters();
            lastExecutedSearchText = searchText;
        }
        searchIsDue = false;
    }

    // ----------------------------------------------------------------------
    /// 検索入力フィールドのセットアップ
    // ----------------------------------------------------------------------
    private void SetupSearchInputField()
    {
        if (searchInputField == null) return;

        InitializeSearchInputField();
        AttachSearchInputListeners();
    }

    // ----------------------------------------------------------------------
    /// 検索入力フィールドの初期設定
    // ----------------------------------------------------------------------
    private void InitializeSearchInputField()
    {
        searchInputField.text = searchText;
    }

    // ----------------------------------------------------------------------
    /// 検索入力フィールドのイベントリスナーを設定
    // ----------------------------------------------------------------------
    private void AttachSearchInputListeners()
    {
        // テキスト変更時のイベント
        searchInputField.onValueChanged.AddListener(OnSearchTextChanged);
        
        // 入力完了時のイベント
        searchInputField.onEndEdit.AddListener(OnSearchTextCompleted);
    }

    // ----------------------------------------------------------------------
    /// 検索テキスト変更時の処理
    /// @param text 変更されたテキスト
    // ----------------------------------------------------------------------
    private void OnSearchTextChanged(string text)
    {
        this.searchText = text;
        RequestSearch();
    }

    // ----------------------------------------------------------------------
    /// 検索テキスト入力完了時の処理
    /// @param text 完了したテキスト
    // ----------------------------------------------------------------------
    private void OnSearchTextCompleted(string text)
    {
        this.searchText = text;
        ExecuteSearchAndFilters();
        lastExecutedSearchText = text;
        CancelDelayedSearch();
    }

    // ----------------------------------------------------------------------
    /// 遅延検索をキャンセル
    // ----------------------------------------------------------------------
    private void CancelDelayedSearch()
    {
        searchIsDue = false;
        currentSearchDelayTimer = 0f;
    }

    // ----------------------------------------------------------------------
    // 検索テキストの設定
    // ----------------------------------------------------------------------
    // ----------------------------------------------------------------------
    /// 検索テキストを設定
    /// @param text 検索テキスト
    // ----------------------------------------------------------------------
    public void SetSearchText(string text)
    {
        if (searchText == text) return;
        
        searchText = text;
        
        if (IsValidSearchText(text))
        {
            RequestSearch();
        }
        else
        {
            RequestSearchWithoutTextFilter();
        }
    }

    // ----------------------------------------------------------------------
    /// 検索テキストが有効かどうかを判定
    /// @param text 判定対象のテキスト
    /// @return 有効な場合true
    // ----------------------------------------------------------------------
    private bool IsValidSearchText(string text)
    {
        return !string.IsNullOrWhiteSpace(text) && 
               text.Trim().Length >= Constants.MIN_SEARCH_LENGTH;
    }

    // ----------------------------------------------------------------------
    /// 検索リクエスト（遅延実行）
    // ----------------------------------------------------------------------
    public void RequestSearch()
    {
        searchIsDue = true;
        currentSearchDelayTimer = Constants.SEARCH_DELAY_SECONDS;
    }

    // ----------------------------------------------------------------------
    /// テキストフィルターなしの検索リクエスト
    // ----------------------------------------------------------------------
    private void RequestSearchWithoutTextFilter()
    {
        ExecuteSearchAndFilters();
    }

    // ----------------------------------------------------------------------
    // ひらがな・カタカナを同一視するための文字列正規化
    // @param input 正規化対象の文字列
    // @return 正規化された文字列（カタカナをひらがなに変換、小文字化）
    // ----------------------------------------------------------------------
    private string NormalizeJapanese(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            // 全角カタカナをひらがなに変換
            if (ch >= Constants.KATAKANA_START && ch <= Constants.KATAKANA_END)
            {
                sb.Append((char)(ch - Constants.KATAKANA_TO_HIRAGANA_OFFSET));
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString().ToLowerInvariant();
    }

    // ----------------------------------------------------------------------
    // Component Cleanup
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// コンポーネント破棄時の処理
    // ----------------------------------------------------------------------
    private void OnDestroy()
    {
        CleanupSearchInputField();
        CleanupSingletonInstance();
    }

    // ----------------------------------------------------------------------
    /// 検索入力フィールドのリスナーを解除
    // ----------------------------------------------------------------------
    private void CleanupSearchInputField()
    {
        if (searchInputField == null) return;

        searchInputField.onEndEdit.RemoveAllListeners();
        searchInputField.onValueChanged.RemoveAllListeners();

        var searchButton = searchInputField.transform.Find(Constants.SEARCH_BUTTON_NAME);
        if (searchButton?.GetComponent<Button>() != null)
        {
            searchButton.GetComponent<Button>().onClick.RemoveAllListeners();
        }
    }

    // ----------------------------------------------------------------------
    /// シングルトンインスタンスの解除
    // ----------------------------------------------------------------------
    private void CleanupSingletonInstance()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    // ----------------------------------------------------------------------
    // Batch Filtering Management
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// フィルタリングの一括処理を開始
    // ----------------------------------------------------------------------
    public void BeginBatchFiltering()
    {
        isBatchFiltering = true;
    }

    // ----------------------------------------------------------------------
    /// フィルタリングの一括処理を終了して適用
    // ----------------------------------------------------------------------
    public void EndBatchFiltering()
    {
        isBatchFiltering = false;
        RequestSearch();
    }

    // ----------------------------------------------------------------------
    // Card Data Loading and Initialization
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// モデルの初期化
    // ----------------------------------------------------------------------
    public void Initialize()
    {
        LoadCards();
        RequestSearch();
    }

    // ----------------------------------------------------------------------
    /// カードデータをロード
    // ----------------------------------------------------------------------
    private void LoadCards()
    {
        var cards = CardDatabase.GetAllCards();
        
        if (cards != null)
        {
            allCards = cards;
            ProcessCardEnumConversion();
            AnalyzeCardDistribution();
        }
        else
        {
            allCards = new List<CardModel>();
        }

        InitializeFilteredCards();
    }

    // ----------------------------------------------------------------------
    /// カードのEnum変換を処理
    // ----------------------------------------------------------------------
    private void ProcessCardEnumConversion()
    {
        int convertedCount = 0;
        foreach (var card in allCards)
        {
            if (ShouldConvertCardEnum(card))
            {
                card.ConvertStringDataToEnums();
                convertedCount++;
            }
        }
    }

    // ----------------------------------------------------------------------
    /// カードがEnum変換を必要とするかどうかを判定
    /// @param card 判定対象のカード
    /// @return 変換が必要な場合true
    // ----------------------------------------------------------------------
    private bool ShouldConvertCardEnum(CardModel card)
    {
        return !string.IsNullOrEmpty(card.cardType);
    }

    // ----------------------------------------------------------------------
    /// カード分布の分析（デバッグ用）
    // ----------------------------------------------------------------------
    private void AnalyzeCardDistribution()
    {
        AnalyzeCardTypeDistribution();
        AnalyzeEvolutionStageDistribution();
        AnalyzePokemonTypeDistribution();
    }

    // ----------------------------------------------------------------------
    /// カードタイプ分布の分析
    // ----------------------------------------------------------------------
    private void AnalyzeCardTypeDistribution()
    {
        var distribution = new Dictionary<CardType, int>();
        foreach (var card in allCards)
        {
            if (!distribution.ContainsKey(card.cardTypeEnum))
                distribution[card.cardTypeEnum] = 0;
            distribution[card.cardTypeEnum]++;
        }
    }

    // ----------------------------------------------------------------------
    /// 進化段階分布の分析
    // ----------------------------------------------------------------------
    private void AnalyzeEvolutionStageDistribution()
    {
        var distribution = new Dictionary<EvolutionStage, int>();
        foreach (var card in allCards)
        {
            if (IsPokemonCard(card))
            {
                if (!distribution.ContainsKey(card.evolutionStageEnum))
                    distribution[card.evolutionStageEnum] = 0;
                distribution[card.evolutionStageEnum]++;
            }
        }
    }

    // ----------------------------------------------------------------------
    /// ポケモンタイプ分布の分析
    // ----------------------------------------------------------------------
    private void AnalyzePokemonTypeDistribution()
    {
        var distribution = new Dictionary<PokemonType, int>();
        foreach (var card in allCards)
        {
            if (IsPokemonCard(card) && !string.IsNullOrEmpty(card.type))
            {
                if (!distribution.ContainsKey(card.typeEnum))
                    distribution[card.typeEnum] = 0;
                distribution[card.typeEnum]++;
            }
        }
    }

    // ----------------------------------------------------------------------
    /// ポケモンカードかどうかを判定
    /// @param card 判定対象のカード
    /// @return ポケモンカードの場合true
    // ----------------------------------------------------------------------
    private bool IsPokemonCard(CardModel card)
    {
        return card.cardTypeEnum == CardType.非EX || card.cardTypeEnum == CardType.EX;
    }

    // ----------------------------------------------------------------------
    /// フィルタリング用カードリストの初期化
    // ----------------------------------------------------------------------
    private void InitializeFilteredCards()
    {
        filteredCards = new List<CardModel>(allCards);
    }

    // ----------------------------------------------------------------------
    /// カードデータを外部から設定する
    /// @param cards 設定するカードデータ
    // ----------------------------------------------------------------------
    public void SetCards(List<CardModel> cards)
    {
        if (cards == null) return;

        allCards = new List<CardModel>(cards);
        ProcessExternalCardEnumConversion();
        ClearAllFilters();
    }

    // ----------------------------------------------------------------------
    /// 外部カードデータのEnum変換を処理
    // ----------------------------------------------------------------------
    private void ProcessExternalCardEnumConversion()
    {
        foreach (var card in allCards)
        {
            if (ShouldConvertExternalCard(card))
            {
                card.ConvertStringDataToEnums();
            }
        }
    }

    // ----------------------------------------------------------------------
    /// 外部カードがEnum変換を必要とするかどうかを判定
    /// @param card 判定対象のカード
    /// @return 変換が必要な場合true
    // ----------------------------------------------------------------------
    private bool ShouldConvertExternalCard(CardModel card)
    {
        return (card.cardTypeEnum == 0 && !string.IsNullOrEmpty(card.cardType)) ||
               (IsPokemonCard(card) && 
                (!string.IsNullOrEmpty(card.evolutionStage) || !string.IsNullOrEmpty(card.type)));
    }

    // ----------------------------------------------------------------------
    // Filter Configuration Methods
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// カードタイプフィルターを設定
    /// @param cardTypes 検索するカードタイプのセット
    // ----------------------------------------------------------------------
    public void SetCardTypeFilter(HashSet<CardType> cardTypes)
    {
        selectedCardTypes = new HashSet<CardType>(cardTypes);
        TriggerFilterUpdate();
    }

    // ----------------------------------------------------------------------
    /// 進化段階フィルターを設定
    /// @param evolutionStages 検索する進化段階のセット
    // ----------------------------------------------------------------------
    public void SetEvolutionStageFilter(HashSet<EvolutionStage> evolutionStages)
    {
        selectedEvolutionStages = new HashSet<EvolutionStage>(evolutionStages);
        TriggerFilterUpdate();
    }

    // ----------------------------------------------------------------------
    /// ポケモンタイプフィルターを設定
    /// @param pokemonTypes 検索するポケモンタイプのセット
    // ----------------------------------------------------------------------
    public void SetPokemonTypeFilter(HashSet<PokemonType> pokemonTypes)
    {
        selectedPokemonTypes = new HashSet<PokemonType>(pokemonTypes);
        TriggerFilterUpdate();
    }

    // ----------------------------------------------------------------------
    /// カードパックフィルターを設定
    /// @param cardPacks 検索するカードパックのセット
    // ----------------------------------------------------------------------
    public void SetCardPackFilter(HashSet<CardPack> cardPacks)
    {
        selectedCardPacks = new HashSet<CardPack>(cardPacks);
        TriggerFilterUpdate();
    }

    // ----------------------------------------------------------------------
    /// HPフィルターを設定
    /// @param hp 検索するHP値
    /// @param comparisonType 比較タイプ（以下、同じ、以上のいずれか）
    // ----------------------------------------------------------------------
    public void SetHPFilter(int hp, SetHPArea.HPComparisonType comparisonType)
    {
        selectedHP = hp;
        selectedHPComparisonType = comparisonType;
        TriggerFilterUpdate();
    }

    // ----------------------------------------------------------------------
    /// 最大ダメージフィルターを設定
    /// @param damage 検索する最大ダメージ値
    /// @param comparisonType 比較タイプ（以下、同じ、以上のいずれか）
    // ----------------------------------------------------------------------
    public void SetMaxDamageFilter(int damage, SetMaxDamageArea.DamageComparisonType comparisonType)
    {
        selectedMaxDamage = damage;
        selectedMaxDamageComparisonType = comparisonType;
        TriggerFilterUpdate();
    }

    // ----------------------------------------------------------------------
    /// 最大エネルギーコストフィルターを設定
    /// @param cost 検索するエネルギーコスト値
    /// @param comparisonType 比較タイプ（以下、同じ、以上のいずれか）
    // ----------------------------------------------------------------------
    public void SetMaxEnergyCostFilter(int cost, SetMaxEnergyArea.EnergyComparisonType comparisonType)
    {
        selectedMaxEnergyCost = cost;
        selectedMaxEnergyCostComparisonType = comparisonType;
        TriggerFilterUpdate();
    }

    // ----------------------------------------------------------------------
    /// 逃げるコストフィルターを設定
    /// @param cost 検索する逃げるコスト値
    /// @param comparisonType 比較タイプ（以下、同じ、以上のいずれか）
    // ----------------------------------------------------------------------
    public void SetRetreatCostFilter(int cost, SetRetreatCostArea.RetreatComparisonType comparisonType)
    {
        selectedRetreatCost = cost;
        selectedRetreatCostComparisonType = comparisonType;
        TriggerFilterUpdate();
    }

    // ----------------------------------------------------------------------
    /// フィルター更新をトリガー（バッチ処理中でなければ検索実行）
    // ----------------------------------------------------------------------
    private void TriggerFilterUpdate()
    {
        if (!isBatchFiltering) RequestSearch();
    }

    // ----------------------------------------------------------------------
    // すべてのフィルターをクリア
    // ----------------------------------------------------------------------
    public void ClearAllFilters()
    {
        searchText = "";
        lastExecutedSearchText = ""; // 重複実行防止変数もリセット
        if (searchInputField != null)
        {
            searchInputField.text = ""; // 紐づけられたInputFieldもクリア
        }
        selectedCardTypes.Clear();
        selectedEvolutionStages.Clear();
        selectedPokemonTypes.Clear();
        selectedCardPacks.Clear();
        // filteredCards = new List<CardModel>(allCards); // ExecuteSearchAndFiltersが処理するので不要

        // HPフィルターをリセット
        selectedHP = 0;
        selectedHPComparisonType = SetHPArea.HPComparisonType.None;

        // 最大ダメージフィルターをリセット
        selectedMaxDamage = 0;
        selectedMaxDamageComparisonType = SetMaxDamageArea.DamageComparisonType.None;

        // エネルギーコストフィルターをリセット
        selectedMaxEnergyCost = 0;
        selectedMaxEnergyCostComparisonType = SetMaxEnergyArea.EnergyComparisonType.None;

        // 逃げるコストフィルターをリセット
        selectedRetreatCost = 0;
        selectedRetreatCostComparisonType = SetRetreatCostArea.RetreatComparisonType.None;

        RequestSearch(); // 表示を更新
    }

    // ----------------------------------------------------------------------
    // フィルタリングと検索を適用 (旧 ApplyFilters)
    // ----------------------------------------------------------------------
    public void ExecuteSearchAndFilters()
    {
        // 最初に全カードをベースにする
        if (allCards == null)
        {
            allCards = new List<CardModel>(); // NullGuard
        }
        filteredCards = new List<CardModel>(allCards);

        // テキスト検索フィルター適用
        ApplyTextFilter();

        // カードタイプフィルター適用
        ApplyCardTypeFilter();

        // 進化段階フィルター適用
        ApplyEvolutionStageFilter();

        // ポケモンタイプフィルター適用
        ApplyPokemonTypeFilter();

        // カードパックフィルター適用
        ApplyCardPackFilter();

        // HPフィルター適用
        ApplyHPFilter();

        // 最大ダメージフィルター適用
        ApplyMaxDamageFilter();

        // 最大エネルギーコストフィルター適用
        ApplyMaxEnergyCostFilter();

        // 逃げるコストフィルター適用
        ApplyRetreatCostFilter();

        // フィルタリング結果のフィードバック表示
        if (filteredCards.Count != allCards.Count)
        {
            FeedbackContainer.Instance.ShowSuccessFeedback(
                string.Format(Constants.SEARCH_RESULT_FORMAT, filteredCards.Count), 
                Constants.FEEDBACK_DISPLAY_DURATION
            );
        }

        // 最後に実行した検索テキストを記録（重複実行防止）
        lastExecutedSearchText = searchText;

        // 検索結果をUIに反映
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.ApplySearchResults(filteredCards);
        }
        else
        {
        }
    }

    // ----------------------------------------------------------------------
    // Filter Application Methods - フィルター適用メソッド群
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// テキスト検索フィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyTextFilter()
    {
        if (string.IsNullOrWhiteSpace(searchText)) return;

        try
        {
            string searchNorm = NormalizeJapanese(searchText);
            filteredCards = filteredCards.Where(card => IsCardMatchingTextFilter(card, searchNorm)).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"テキストフィルター適用中にエラーが発生しました: {ex.Message}");
            // エラーが発生した場合はフィルターをスキップ
        }
    }

    // ----------------------------------------------------------------------
    /// カードがテキストフィルターにマッチするかを判定
    /// @param card 判定対象のカード
    /// @param normalizedSearchText 正規化された検索テキスト
    /// @return マッチする場合true
    // ----------------------------------------------------------------------
    private bool IsCardMatchingTextFilter(CardModel card, string normalizedSearchText)
    {
        if (card == null) return false;

        // カード名マッチ
        if (!string.IsNullOrEmpty(card.name) && 
            NormalizeJapanese(card.name).Contains(normalizedSearchText))
            return true;

        // 特性効果マッチ
        if (!string.IsNullOrEmpty(card.abilityEffect) && 
            NormalizeJapanese(card.abilityEffect).Contains(normalizedSearchText))
            return true;

        // 技効果マッチ
        if (card.moves != null)
        {
            foreach (var move in card.moves)
            {
                if (!string.IsNullOrEmpty(move.effect) && 
                    NormalizeJapanese(move.effect).Contains(normalizedSearchText))
                    return true;
            }
        }

        return false;
    }

    // ----------------------------------------------------------------------
    /// カードタイプフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyCardTypeFilter()
    {
        if (selectedCardTypes.Count == 0) return;

        try
        {
            filteredCards = filteredCards.Where(card =>
                card != null && selectedCardTypes.Contains(card.cardTypeEnum)
            ).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"カードタイプフィルター適用中にエラーが発生しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    /// 進化段階フィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyEvolutionStageFilter()
    {
        if (selectedEvolutionStages.Count == 0) return;

        try
        {
            filteredCards = filteredCards.Where(card =>
                card != null &&
                IsPokemonCard(card) &&
                !string.IsNullOrEmpty(card.evolutionStage) &&
                selectedEvolutionStages.Contains(card.evolutionStageEnum)
            ).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"進化段階フィルター適用中にエラーが発生しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    /// ポケモンタイプフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyPokemonTypeFilter()
    {
        if (selectedPokemonTypes.Count == 0) return;

        try
        {
            filteredCards = filteredCards.Where(card =>
                card != null &&
                IsPokemonCard(card) &&
                !string.IsNullOrEmpty(card.type) &&
                selectedPokemonTypes.Contains(card.typeEnum)
            ).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ポケモンタイプフィルター適用中にエラーが発生しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    /// カードパックフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyCardPackFilter()
    {
        if (selectedCardPacks.Count == 0) return;

        try
        {
            filteredCards = filteredCards.Where(card =>
                card != null &&
                !string.IsNullOrEmpty(card.pack) &&
                selectedCardPacks.Contains(card.packEnum)
            ).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"カードパックフィルター適用中にエラーが発生しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    /// HPフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyHPFilter()
    {
        if (selectedHPComparisonType == SetHPArea.HPComparisonType.None || selectedHP <= 0)
            return;

        try
        {
            filteredCards = filteredCards.Where(card =>
                card != null && card.hp > 0 && EvaluateNumericComparison(card.hp, selectedHP, selectedHPComparisonType)
            ).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"HPフィルター適用中にエラーが発生しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    /// 最大ダメージフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyMaxDamageFilter()
    {
        if (selectedMaxDamageComparisonType == SetMaxDamageArea.DamageComparisonType.None)
            return;

        try
        {
            filteredCards = filteredCards.Where(card =>
                card != null && EvaluateNumericComparison(card.maxDamage, selectedMaxDamage, selectedMaxDamageComparisonType)
            ).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"最大ダメージフィルター適用中にエラーが発生しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    /// 最大エネルギーコストフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyMaxEnergyCostFilter()
    {
        if (selectedMaxEnergyCostComparisonType == SetMaxEnergyArea.EnergyComparisonType.None)
            return;

        try
        {
            filteredCards = filteredCards.Where(card =>
                card != null && EvaluateNumericComparison(card.maxEnergyCost, selectedMaxEnergyCost, selectedMaxEnergyCostComparisonType)
            ).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"最大エネルギーコストフィルター適用中にエラーが発生しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    /// 逃げるコストフィルターの適用
    // ----------------------------------------------------------------------
    private void ApplyRetreatCostFilter()
    {
        if (selectedRetreatCostComparisonType == SetRetreatCostArea.RetreatComparisonType.None)
            return;

        try
        {
            filteredCards = filteredCards.Where(card =>
                card != null &&
                IsPokemonCard(card) &&
                EvaluateNumericComparison(card.retreatCost, selectedRetreatCost, selectedRetreatCostComparisonType)
            ).ToList();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"逃げるコストフィルター適用中にエラーが発生しました: {ex.Message}");
        }
    }

    // ----------------------------------------------------------------------
    // Filter Helper Methods - フィルターヘルパーメソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// 数値比較を評価（HPフィルター用）
    /// @param cardValue カードの値
    /// @param filterValue フィルター値
    /// @param comparisonType 比較タイプ
    /// @return 比較条件にマッチする場合true
    // ----------------------------------------------------------------------
    private bool EvaluateNumericComparison(int cardValue, int filterValue, SetHPArea.HPComparisonType comparisonType)
    {
        switch (comparisonType)
        {
            case SetHPArea.HPComparisonType.LessOrEqual:
                return cardValue <= filterValue;
            case SetHPArea.HPComparisonType.Equal:
                return cardValue == filterValue;
            case SetHPArea.HPComparisonType.GreaterOrEqual:
                return cardValue >= filterValue;
            default:
                return false;
        }
    }

    // ----------------------------------------------------------------------
    /// 数値比較を評価（最大ダメージフィルター用）
    /// @param cardValue カードの値
    /// @param filterValue フィルター値
    /// @param comparisonType 比較タイプ
    /// @return 比較条件にマッチする場合true
    // ----------------------------------------------------------------------
    private bool EvaluateNumericComparison(int cardValue, int filterValue, SetMaxDamageArea.DamageComparisonType comparisonType)
    {
        switch (comparisonType)
        {
            case SetMaxDamageArea.DamageComparisonType.LessOrEqual:
                return cardValue <= filterValue;
            case SetMaxDamageArea.DamageComparisonType.Equal:
                return cardValue == filterValue;
            case SetMaxDamageArea.DamageComparisonType.GreaterOrEqual:
                return cardValue >= filterValue;
            default:
                return false;
        }
    }

    // ----------------------------------------------------------------------
    /// 数値比較を評価（最大エネルギーコストフィルター用）
    /// @param cardValue カードの値
    /// @param filterValue フィルター値
    /// @param comparisonType 比較タイプ
    /// @return 比較条件にマッチする場合true
    // ----------------------------------------------------------------------
    private bool EvaluateNumericComparison(int cardValue, int filterValue, SetMaxEnergyArea.EnergyComparisonType comparisonType)
    {
        switch (comparisonType)
        {
            case SetMaxEnergyArea.EnergyComparisonType.LessOrEqual:
                return cardValue <= filterValue;
            case SetMaxEnergyArea.EnergyComparisonType.Equal:
                return cardValue == filterValue;
            case SetMaxEnergyArea.EnergyComparisonType.GreaterOrEqual:
                return cardValue >= filterValue;
            default:
                return true; // フィルタリングしない
        }
    }

    // ----------------------------------------------------------------------
    /// 数値比較を評価（逃げるコストフィルター用）
    /// @param cardValue カードの値
    /// @param filterValue フィルター値
    /// @param comparisonType 比較タイプ
    /// @return 比較条件にマッチする場合true
    // ----------------------------------------------------------------------
    private bool EvaluateNumericComparison(int cardValue, int filterValue, SetRetreatCostArea.RetreatComparisonType comparisonType)
    {
        switch (comparisonType)
        {
            case SetRetreatCostArea.RetreatComparisonType.LessOrEqual:
                return cardValue <= filterValue;
            case SetRetreatCostArea.RetreatComparisonType.Equal:
                return cardValue == filterValue;
            case SetRetreatCostArea.RetreatComparisonType.GreaterOrEqual:
                return cardValue >= filterValue;
            default:
                return true; // フィルタリングしない
        }
    }

    // ----------------------------------------------------------------------
    // 現在のフィルタリング結果を取得
    // @return フィルタリングされたカードリスト
    // ----------------------------------------------------------------------
    public List<CardModel> GetFilteredCards()
    {
        return new List<CardModel>(filteredCards);
    }

    // ----------------------------------------------------------------------
    // 現在のフィルタリング条件を取得
    // ----------------------------------------------------------------------
    public List<CardModel> Search(
        List<CardType> cardTypes,
        List<EvolutionStage> evolutionStages,
        List<PokemonType> types,
        List<CardPack> cardPacks,
        int minHP,
        int maxHP,
        int minMaxDamage,
        int maxMaxDamage,
        int minEnergyCost,
        int maxEnergyCost,
        int minRetreatCost,
        int maxRetreatCost
    )
    {

        // 検索条件の有無を適切にチェック
        bool hasCardTypeFilter = cardTypes != null && cardTypes.Count > 0;
        bool hasEvolutionStageFilter = evolutionStages != null && evolutionStages.Count > 0;
        bool hasTypeFilter = types != null && types.Count > 0;
        bool hasCardPackFilter = cardPacks != null && cardPacks.Count > 0;

        // デフォルト値を使用してフィルター条件を判定
        bool hasHPFilter = minHP > Constants.DEFAULT_HP_MIN || maxHP < Constants.DEFAULT_HP_MAX;
        bool hasMaxDamageFilter = minMaxDamage > Constants.DEFAULT_DAMAGE_MIN || maxMaxDamage < Constants.DEFAULT_DAMAGE_MAX;
        bool hasEnergyCostFilter = minEnergyCost > Constants.DEFAULT_ENERGY_COST_MIN || maxEnergyCost < Constants.DEFAULT_ENERGY_COST_MAX;
        bool hasRetreatCostFilter = minRetreatCost > Constants.DEFAULT_RETREAT_COST_MIN || maxRetreatCost < Constants.DEFAULT_RETREAT_COST_MAX;

        // カードリストが直接設定されている場合はそれを使用
        List<CardModel> allCards = null;
        if (cardList != null && cardList.Count > 0)
        {
            allCards = cardList;
        }
        // それ以外の場合はCardDatabaseから取得
        else if (CardDatabase.Instance != null)
        {
            allCards = CardDatabase.GetAllCards();
            if (allCards != null)
            {
            }
            else
            {
                return new List<CardModel>();
            }
        }
        else
        {
            return new List<CardModel>();
        }

        // フィルターの適用
        var filteredCards = allCards.Where(card =>
        {
            // フィルター条件がない場合は全カード表示（条件は AND 条件で、フィルターがなければ自動的に true）
            bool matchCardType = !hasCardTypeFilter || cardTypes.Contains(card.cardTypeEnum);
            bool matchEvolutionStage = !hasEvolutionStageFilter || evolutionStages.Contains(card.evolutionStageEnum);
            bool matchType = !hasTypeFilter || types.Contains(card.typeEnum);
            bool matchCardPack = !hasCardPackFilter || cardPacks.Contains(card.packEnum);

            // 数値フィルターの条件判定を修正
            bool matchHP = !hasHPFilter || (card.hp >= minHP && card.hp <= maxHP);
            bool matchMaxDamage = !hasMaxDamageFilter || (card.maxDamage >= minMaxDamage && card.maxDamage <= maxMaxDamage);
            bool matchEnergyCost = !hasEnergyCostFilter || (card.maxEnergyCost >= minEnergyCost && card.maxEnergyCost <= maxEnergyCost);
            bool matchRetreatCost = !hasRetreatCostFilter || (card.retreatCost >= minRetreatCost && card.retreatCost <= maxRetreatCost);

            // すべての条件にマッチするか（AND条件）
            return matchCardType && matchEvolutionStage && matchType && matchCardPack
                && matchHP && matchMaxDamage && matchEnergyCost && matchRetreatCost;
        }).ToList();

        return filteredCards;
    }

    // ----------------------------------------------------------------------
    // Text Search Methods - テキスト検索関連メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    /// テキスト検索と現在のフィルターを組み合わせて実行
    /// @param searchText 検索テキスト
    // ----------------------------------------------------------------------
    public void PerformTextSearchAndFilter(string searchText)
    {
        this.searchText = searchText;
        ExecuteSearchAndFilters();
        
        var currentlyFilteredCards = new List<CardModel>(this.filteredCards);
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            DisplaySearchResults(currentlyFilteredCards);
            return;
        }

        var searchResults = ExecuteTextSearch(searchText, currentlyFilteredCards);
        DisplaySearchResults(searchResults);
    }

    // ----------------------------------------------------------------------
    /// テキスト検索を実行
    /// @param searchText 検索テキスト
    /// @param targetCards 検索対象のカードリスト
    /// @return 検索結果のカードリスト
    // ----------------------------------------------------------------------
    private List<CardModel> ExecuteTextSearch(string searchText, List<CardModel> targetCards)
    {
        string searchNorm = NormalizeJapanese(searchText);
        var results = new List<CardModel>();

        foreach (var card in targetCards)
        {
            if (IsCardMatchingSearchText(card, searchNorm))
            {
                results.Add(card);
            }
        }

        return results;
    }

    // ----------------------------------------------------------------------
    /// カードが検索テキストにマッチするかどうかを判定
    /// @param card 判定対象のカード
    /// @param normalizedSearchText 正規化された検索テキスト
    /// @return マッチする場合true
    // ----------------------------------------------------------------------
    private bool IsCardMatchingSearchText(CardModel card, string normalizedSearchText)
    {
        // カード名チェック
        if (IsCardNameMatching(card, normalizedSearchText))
            return true;

        // 特性効果テキストチェック
        if (IsAbilityEffectMatching(card, normalizedSearchText))
            return true;

        // 技効果テキストチェック
        if (IsMoveEffectMatching(card, normalizedSearchText))
            return true;

        return false;
    }

    // ----------------------------------------------------------------------
    /// カード名が検索テキストにマッチするかを判定
    /// @param card 判定対象のカード
    /// @param normalizedSearchText 正規化された検索テキスト
    /// @return マッチする場合true
    // ----------------------------------------------------------------------
    private bool IsCardNameMatching(CardModel card, string normalizedSearchText)
    {
        if (string.IsNullOrEmpty(card.name)) return false;
        
        var nameNorm = NormalizeJapanese(card.name);
        return nameNorm.Contains(normalizedSearchText);
    }

    // ----------------------------------------------------------------------
    /// 特性効果が検索テキストにマッチするかを判定
    /// @param card 判定対象のカード
    /// @param normalizedSearchText 正規化された検索テキスト
    /// @return マッチする場合true
    // ----------------------------------------------------------------------
    private bool IsAbilityEffectMatching(CardModel card, string normalizedSearchText)
    {
        if (string.IsNullOrEmpty(card.abilityEffect)) return false;
        
        var abilityEffectNorm = NormalizeJapanese(card.abilityEffect);
        return abilityEffectNorm.Contains(normalizedSearchText);
    }

    // ----------------------------------------------------------------------
    /// 技効果が検索テキストにマッチするかを判定
    /// @param card 判定対象のカード
    /// @param normalizedSearchText 正規化された検索テキスト
    /// @return マッチする場合true
    // ----------------------------------------------------------------------
    private bool IsMoveEffectMatching(CardModel card, string normalizedSearchText)
    {
        if (card.moves == null) return false;

        foreach (var move in card.moves)
        {
            if (string.IsNullOrEmpty(move.effect)) continue;
            
            var effectNorm = NormalizeJapanese(move.effect);
            if (effectNorm.Contains(normalizedSearchText))
                return true;
        }

        return false;
    }

    // ----------------------------------------------------------------------
    /// 検索結果をUIに表示
    /// @param results 表示する検索結果
    // ----------------------------------------------------------------------
    private void DisplaySearchResults(List<CardModel> results)
    {
        if (SearchNavigator.Instance != null)
        {
            SearchNavigator.Instance.ApplySearchResults(results);
        }
    }
}