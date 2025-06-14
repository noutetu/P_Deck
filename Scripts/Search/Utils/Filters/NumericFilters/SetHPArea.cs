using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PokeDeck.Search.Filters.Base;

// ----------------------------------------------------------------------
// HPのフィルタリングを担当するPresenter
// BaseNumericFilterAreaを継承して共通ロジックを利用
// ----------------------------------------------------------------------
public class SetHPArea : BaseNumericFilterArea<SetHPArea.HPComparisonType>
{
    // ======================================================================
    // Constants / 定数
    // ======================================================================
    private static class Constants
    {
        public const int HP_MIN_VALUE = 30;
        public const int HP_MAX_VALUE = 200;
        public const int HP_INCREMENT = 10;
        public const string DEFAULT_TEXT = "指定なし";
    }
    
    // ======================================================================
    // Enums / 列挙型
    // ======================================================================
    public enum HPComparisonType
    {
        None,          // 比較なし（選択していない状態）
        LessOrEqual,   // 以下
        Equal,         // 同じ
        GreaterOrEqual // 以上
    }
    
    // ======================================================================
    // Fields / フィールド
    // ======================================================================
    [Header("HP比較トグル")]
    [SerializeField] private Toggle lessOrEqualToggle;    // 以下トグル
    [SerializeField] private Toggle equalToggle;          // 同じトグル
    [SerializeField] private Toggle greaterOrEqualToggle; // 以上トグル
    
    // ======================================================================
    // Abstract Methods Implementation / 抽象メソッドの実装
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // 比較トグルマッピングの初期化
    // ----------------------------------------------------------------------
    protected override void InitializeComparisonToggleMappings()
    {
        validateToggleReferences();
        setupToggleMapping();
    }

    // ----------------------------------------------------------------------
    // ドロップダウンオプションの設定
    // ----------------------------------------------------------------------
    protected override void PopulateDropdownOptions()
    {
        if (!validateDropdownReference()) return;
        
        clearAndSetupDropdownOptions();
    }

    protected override HPComparisonType GetDefaultComparisonType()
    {
        return HPComparisonType.None;
    }

    protected override string GetDropdownDefaultText()
    {
        return Constants.DEFAULT_TEXT;
    }

    protected override int GetDefaultNumericValue()
    {
        return 0;
    }

    protected override bool TryParseDropdownValue(string text, out int value)
    {
        if (text == GetDropdownDefaultText())
        {
            value = GetDefaultNumericValue();
            return true; 
        }
        return int.TryParse(text, out value);
    }

    protected override Toggle GetDefaultComparisonToggleForDropdownInteraction()
    {
        return equalToggle;
    }
    
    // ----------------------------------------------------------------------
    // 比較トグルの視覚状態更新
    // ----------------------------------------------------------------------
    protected override void UpdateComparisonToggleVisualState(Toggle toggle, bool isOn)
    {
        if (toggle == null) return;

        try
        {
            updateToggleInteractability(toggle);
            updateToggleColorState(toggle, isOn);
            updateToggleShadowState(toggle, isOn);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Exception in UpdateComparisonToggleVisualState for SetHPArea: {ex.Message}");
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 追加のトグル設定
    // ----------------------------------------------------------------------
    protected override void performAdditionalToggleSetup()
    {
        ToggleGroup toggleGroup = createToggleGroup();
        assignTogglesToGroup(toggleGroup);
        setInitialToggleState();
        setAllTogglesInteractable(false);
    }

    // ======================================================================
    // Event Handlers / イベントハンドラー
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // ドロップダウン値変更時の追加処理
    // ----------------------------------------------------------------------
    protected override void OnDropdownValueChanged(int index)
    {
        base.OnDropdownValueChanged(index);
        updateToggleStateBasedOnDropdownSelection();
    }

    // ======================================================================
    // Public API / 公開メソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // フィルター適用ロジック
    // ----------------------------------------------------------------------
    public override void ApplyFilterToModel(SearchModel model)
    {
        this.searchModel = model;
        
        if (this.searchModel == null) 
        {
            Debug.LogError("SearchModel is not initialized in SetHPArea.");
            return;
        }

        applyHPFilterToModel();
    }

    // ----------------------------------------------------------------------
    // フィルターのリセット
    // ----------------------------------------------------------------------
    public override void ResetFilters()
    {
        base.ResetFilters();
        setAllTogglesInteractable(false);
    }

    // ----------------------------------------------------------------------
    // 公開プロパティ（互換性のため）
    // ----------------------------------------------------------------------
    public int GetSelectedHP() => selectedValue;
    public HPComparisonType GetSelectedComparisonType() => selectedComparisonType;
    public bool HasActiveFilters() => IsFilterEffectivelyActive();

    // ======================================================================
    // Helper Methods / ヘルパーメソッド
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // トグル参照の検証
    // ----------------------------------------------------------------------
    private void validateToggleReferences()
    {
        if (lessOrEqualToggle == null || equalToggle == null || greaterOrEqualToggle == null)
        {
            Debug.LogError("One or more toggle references are null in SetHPArea. Check Inspector assignments.");
        }
    }
    
    // ----------------------------------------------------------------------
    // トグルマッピングの設定
    // ----------------------------------------------------------------------
    private void setupToggleMapping()
    {
        comparisonToggleMap = new Dictionary<Toggle, HPComparisonType>();
        
        // null チェックを行ってからマッピングに追加
        AddToggleIfNotNull(lessOrEqualToggle, HPComparisonType.LessOrEqual);
        AddToggleIfNotNull(equalToggle, HPComparisonType.Equal);
        AddToggleIfNotNull(greaterOrEqualToggle, HPComparisonType.GreaterOrEqual);
    }

    private void AddToggleIfNotNull(Toggle toggle, HPComparisonType comparisonType)
    {
        if (toggle != null)
        {
            comparisonToggleMap.Add(toggle, comparisonType);
        }
        else
        {
            Debug.LogWarning($"Toggle for {comparisonType} is null in SetHPArea. Check Inspector assignments.");
        }
    }
    
    // ----------------------------------------------------------------------
    // ドロップダウン参照の検証
    // ----------------------------------------------------------------------
    private bool validateDropdownReference()
    {
        if (valueDropdown == null) 
        {
            Debug.LogError("valueDropdown is not assigned in SetHPArea. Check Inspector assignment.");
            return false;
        }
        return true;
    }
    
    // ----------------------------------------------------------------------
    // ドロップダウンオプションのクリアと設定
    // ----------------------------------------------------------------------
    private void clearAndSetupDropdownOptions()
    {
        valueDropdown.ClearOptions();
        
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>
        {
            new Dropdown.OptionData(GetDropdownDefaultText())
        };
        
        addHPOptions(options);
        
        valueDropdown.AddOptions(options);
        valueDropdown.value = 0;
        valueDropdown.RefreshShownValue();
    }
    
    // ----------------------------------------------------------------------
    // HPオプションの追加
    // ----------------------------------------------------------------------
    private void addHPOptions(List<Dropdown.OptionData> options)
    {
        for (int hp = Constants.HP_MIN_VALUE; hp <= Constants.HP_MAX_VALUE; hp += Constants.HP_INCREMENT)
        {
            options.Add(new Dropdown.OptionData(hp.ToString()));
        }
    }
    
    // ----------------------------------------------------------------------
    // トグルのInteractability更新
    // ----------------------------------------------------------------------
    private void updateToggleInteractability(Toggle toggle)
    {
        bool shouldBeInteractable = selectedDropdownText != GetDropdownDefaultText();
        toggle.interactable = shouldBeInteractable;
    }
    
    // ----------------------------------------------------------------------
    // トグルの色状態更新
    // ----------------------------------------------------------------------
    private void updateToggleColorState(Toggle toggle, bool isOn)
    {
        SimpleToggleColor colorComponent = toggle.GetComponent<SimpleToggleColor>();
        colorComponent?.UpdateColorState(isOn);
    }
    
    // ----------------------------------------------------------------------
    // トグルの影状態更新
    // ----------------------------------------------------------------------
    private void updateToggleShadowState(Toggle toggle, bool isOn)
    {
        TrueShadowToggleInset shadowComponent = toggle.GetComponent<TrueShadowToggleInset>();
        shadowComponent?.UpdateInsetState(isOn);
    }
    
    // ----------------------------------------------------------------------
    // ToggleGroupの作成
    // ----------------------------------------------------------------------
    private ToggleGroup createToggleGroup()
    {
        ToggleGroup toggleGroup = gameObject.AddComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = true;
        return toggleGroup;
    }
    
    // ----------------------------------------------------------------------
    // トグルをグループに割り当て
    // ----------------------------------------------------------------------
    private void assignTogglesToGroup(ToggleGroup toggleGroup)
    {
        if (lessOrEqualToggle != null) lessOrEqualToggle.group = toggleGroup;
        if (equalToggle != null) equalToggle.group = toggleGroup;
        if (greaterOrEqualToggle != null) greaterOrEqualToggle.group = toggleGroup;
    }
    
    // ----------------------------------------------------------------------
    // 初期トグル状態の設定
    // ----------------------------------------------------------------------
    private void setInitialToggleState()
    {
        if (equalToggle != null) 
        {
            equalToggle.SetIsOnWithoutNotify(true);
        }
    }
    
    // ----------------------------------------------------------------------
    // 全トグルのInteractableを設定
    // ----------------------------------------------------------------------
    private void setAllTogglesInteractable(bool interactable)
    {
        if (lessOrEqualToggle != null) lessOrEqualToggle.interactable = interactable;
        if (equalToggle != null) equalToggle.interactable = interactable;
        if (greaterOrEqualToggle != null) greaterOrEqualToggle.interactable = interactable;
    }
    
    // ----------------------------------------------------------------------
    // ドロップダウン選択に基づくトグル状態更新
    // ----------------------------------------------------------------------
    private void updateToggleStateBasedOnDropdownSelection()
    {
        bool shouldEnableToggles = selectedDropdownText != GetDropdownDefaultText();
        setAllTogglesInteractable(shouldEnableToggles);
        
        if (!shouldEnableToggles)
        {
            resetAllToggles();
        }
    }
    
    // ----------------------------------------------------------------------
    // 全トグルのリセット
    // ----------------------------------------------------------------------
    private void resetAllToggles()
    {
        foreach (KeyValuePair<Toggle, HPComparisonType> entry in comparisonToggleMap)
        {
            if (entry.Key != null)
            {
                entry.Key.SetIsOnWithoutNotify(false);
                UpdateComparisonToggleVisualState(entry.Key, false);
            }
        }
    }
    
    // ----------------------------------------------------------------------
    // HPフィルターのモデル適用
    // ----------------------------------------------------------------------
    private void applyHPFilterToModel()
    {
        if (IsFilterEffectivelyActive())
        {
            this.searchModel.SetHPFilter(selectedValue, selectedComparisonType);
        }
        else
        {
            this.searchModel.SetHPFilter(GetDefaultNumericValue(), HPComparisonType.None);
        }
    }

    // ======================================================================
    // Utility / ユーティリティ
    // ======================================================================
    
    // ----------------------------------------------------------------------
    // テスト用公開メソッド（互換性のため）
    // ----------------------------------------------------------------------
#if DEVELOPMENT_BUILD || UNITY_EDITOR
    public new void ManuallyInitialize() => base.ManuallyInitialize();
    public new void CheckDropdownState() => base.CheckDropdownState();
    public void ManuallyPopulateDropdown() => PopulateDropdownOptions();
#endif
}