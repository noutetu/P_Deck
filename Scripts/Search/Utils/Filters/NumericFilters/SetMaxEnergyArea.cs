using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PokeDeck.Search.Filters.Base; // BaseNumericFilterAreaの名前空間をインポート

// ----------------------------------------------------------------------
// 最大エネルギーコストのフィルタリングを担当するPresenter
// BaseNumericFilterAreaを継承して共通ロジックを利用
// ----------------------------------------------------------------------
public class SetMaxEnergyArea : BaseNumericFilterArea<SetMaxEnergyArea.EnergyComparisonType>
{
    // ======================================================================
    // Constants / 定数
    // ======================================================================
    private static class Constants
    {
        public const int ENERGY_MIN_VALUE = 0;
        public const int ENERGY_MAX_VALUE = 5;
        public const int ENERGY_INCREMENT = 1;
        public const string DEFAULT_TEXT = "指定なし";
    }
    
    // ======================================================================
    // Enums / 列挙型
    // ======================================================================
    public enum EnergyComparisonType
    {
        None,           // 比較なし（選択していない状態）
        LessOrEqual,    // 以下
        Equal,          // 同じ
        GreaterOrEqual  // 以上
    }
    
    // ======================================================================
    // Fields / フィールド
    // ======================================================================
    
    [Header("エネルギーコスト比較トグル")]
    [SerializeField] private Toggle lessOrEqualToggle;    // 以下トグル
    [SerializeField] private Toggle equalToggle;          // 同じトグル
    [SerializeField] private Toggle greaterOrEqualToggle; // 以上トグル
    
    // valueDropdownは基底クラスBaseNumericFilterAreaで定義されているため削除
    // selectedComparisonType、selectedEnergyCost、OnFilterChangedは基底クラスで管理されるため削除
    
    // ----------------------------------------------------------------------
    // 基底クラスの抽象メソッドの実装
    // ----------------------------------------------------------------------
    
    protected override void InitializeComparisonToggleMappings()
    {
        comparisonToggleMap = new Dictionary<Toggle, EnergyComparisonType>();
        
        // null チェックを行ってからマッピングに追加
        AddToggleIfNotNull(lessOrEqualToggle, EnergyComparisonType.LessOrEqual);
        AddToggleIfNotNull(equalToggle, EnergyComparisonType.Equal);
        AddToggleIfNotNull(greaterOrEqualToggle, EnergyComparisonType.GreaterOrEqual);
    }

    private void AddToggleIfNotNull(Toggle toggle, EnergyComparisonType comparisonType)
    {
        if (toggle != null)
        {
            comparisonToggleMap.Add(toggle, comparisonType);
        }
        else
        {
            Debug.LogWarning($"Toggle for {comparisonType} is null in SetMaxEnergyArea. Check Inspector assignments.");
        }
    }

    protected override void PopulateDropdownOptions()
    {
        if (valueDropdown == null) 
        {
            Debug.LogError("valueDropdown is not assigned in SetMaxEnergyArea. Make sure it's assigned in the Inspector.");
            return;
        }

        valueDropdown.ClearOptions();
        List<Dropdown.OptionData> options = new List<Dropdown.OptionData>
        {
            new Dropdown.OptionData(GetDropdownDefaultText()) // "指定なし"
        };
        
        // エネルギーコスト値のオプションを追加
        for (int cost = Constants.ENERGY_MIN_VALUE + 1; cost <= Constants.ENERGY_MAX_VALUE; cost += Constants.ENERGY_INCREMENT)
        {
            options.Add(new Dropdown.OptionData(cost.ToString()));
        }
        
        valueDropdown.AddOptions(options);
        valueDropdown.value = 0; // 初期値は「指定なし」
        valueDropdown.RefreshShownValue();
    }

    protected override EnergyComparisonType GetDefaultComparisonType()
    {
        return EnergyComparisonType.None; // デフォルトは比較なし
    }

    protected override string GetDropdownDefaultText()
    {
        return Constants.DEFAULT_TEXT;
    }

    protected override int GetDefaultNumericValue()
    {
        return 0; // 「指定なし」の場合のエネルギーコスト値
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
        return equalToggle; // ドロップダウン操作時は「同じ」トグルをデフォルトでON
    }
    
    protected override void UpdateComparisonToggleVisualState(Toggle toggle, bool isOn)
    {
        if (toggle == null) return;

        // トグルの有効/無効状態を管理（エネルギーコストが選択されていない場合は無効化）
        bool shouldBeInteractable = selectedDropdownText != GetDropdownDefaultText();
        toggle.interactable = shouldBeInteractable;

        SimpleToggleColor colorComponent = toggle.GetComponent<SimpleToggleColor>();
        if (colorComponent != null)
        {
            colorComponent.UpdateColorState(isOn);
        }

        TrueShadowToggleInset shadowComponent = toggle.GetComponent<TrueShadowToggleInset>();
        if (shadowComponent != null)
        {
            shadowComponent.UpdateInsetState(isOn);
        }
    }

    // ----------------------------------------------------------------------
    // ToggleGroupの設定をオーバーライド
    // ----------------------------------------------------------------------
    protected override void performAdditionalToggleSetup()
    {
        // SetMaxEnergyArea固有のToggleGroup設定
        ToggleGroup toggleGroup = gameObject.AddComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = true;  // グループ内のすべてのトグルをオフにできるようにする
        
        // 各トグルをトグルグループに追加
        if (lessOrEqualToggle != null) lessOrEqualToggle.group = toggleGroup;
        if (equalToggle != null) 
        {
            equalToggle.group = toggleGroup;
            // デフォルトで「同じ」トグルを選択状態にしておく（ただし、まだ有効化しない）
            equalToggle.SetIsOnWithoutNotify(true);
            // 視覚的状態も更新
            UpdateComparisonToggleVisualState(equalToggle, true);
        }
        if (greaterOrEqualToggle != null) greaterOrEqualToggle.group = toggleGroup;
        
        // 初期状態ではトグルを無効化（エネルギーコストが選択されていないため）
        SetAllTogglesInteractable(false);
    }

    // ----------------------------------------------------------------------
    // 全トグルの有効/無効を一括設定するヘルパーメソッド
    // ----------------------------------------------------------------------
    private void SetAllTogglesInteractable(bool interactable)
    {
        if (lessOrEqualToggle != null) lessOrEqualToggle.interactable = interactable;
        if (equalToggle != null) equalToggle.interactable = interactable;
        if (greaterOrEqualToggle != null) greaterOrEqualToggle.interactable = interactable;
    }

    // ----------------------------------------------------------------------
    // ドロップダウン値変更時の追加処理をオーバーライド
    // ----------------------------------------------------------------------
    protected override void OnDropdownValueChanged(int index)
    {
        base.OnDropdownValueChanged(index); // 基底クラスの処理を実行
        
        // SetMaxEnergyArea固有の処理: トグルの有効/無効状態を更新
        bool shouldEnableToggles = selectedDropdownText != GetDropdownDefaultText();
        SetAllTogglesInteractable(shouldEnableToggles);
        
        if (!shouldEnableToggles)
        {
            // 「指定なし」の場合はすべてのトグルをオフにする
            foreach (var entry in comparisonToggleMap)
            {
                if (entry.Key != null)
                {
                    entry.Key.SetIsOnWithoutNotify(false);
                    UpdateComparisonToggleVisualState(entry.Key, false);
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // フィルター適用ロジック (IFilterAreaから)
    // ----------------------------------------------------------------------
    public override void ApplyFilterToModel(SearchModel model)
    {
        this.searchModel = model;
        
        if (this.searchModel == null) 
        {
            Debug.LogError("SearchModel is not initialized in SetMaxEnergyArea.");
            return;
        }

        if (IsFilterEffectivelyActive())
        {
            this.searchModel.SetMaxEnergyCostFilter(selectedValue, selectedComparisonType);
        }
        else
        {
            this.searchModel.SetMaxEnergyCostFilter(GetDefaultNumericValue(), EnergyComparisonType.None);
        }
    }

    // ----------------------------------------------------------------------
    // フィルターのリセット (IFilterAreaから)
    // ----------------------------------------------------------------------
    public override void ResetFilters()
    {
        base.ResetFilters(); // 基底クラスのリセット処理
        
        // SetMaxEnergyArea固有のリセット処理: トグルを無効化
        SetAllTogglesInteractable(false);
    }

    // ----------------------------------------------------------------------
    // 元の公開メソッドを維持（互換性のため）
    // ----------------------------------------------------------------------
    public int GetSelectedEnergyCost()
    {
        return selectedValue; // 基底クラスのselectedValueを使用
    }

    public EnergyComparisonType GetSelectedComparisonType()
    {
        return selectedComparisonType; // 基底クラスのselectedComparisonTypeを使用
    }

    public bool HasActiveFilters()
    {
        return IsFilterEffectivelyActive(); // 基底クラスのメソッドを使用
    }
    
    /// <summary>
    /// 手動で初期化を実行するメソッド（テスト用）
    /// </summary>
#if DEVELOPMENT_BUILD || UNITY_EDITOR
    public new void ManuallyInitialize()
    {
        Debug.Log($"SetMaxEnergyArea.ManuallyInitialize called. valueDropdown is null: {valueDropdown == null}");
        base.ManuallyInitialize();
    }
    
    /// <summary>
    /// valueDropdownの状態を確認するメソッド（テスト用）
    /// </summary>
    public new void CheckDropdownState()
    {
        Debug.Log($"SetMaxEnergyArea.CheckDropdownState called");
        base.CheckDropdownState();
    }
#endif
}
