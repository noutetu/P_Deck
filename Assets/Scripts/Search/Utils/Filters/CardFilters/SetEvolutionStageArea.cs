using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Enum;
using PokeDeck.Search.Filters.Base; // BaseToggleFilterAreaの名前空間をインポート

// ----------------------------------------------------------------------
// 進化段階のフィルタリングを担当するPresenter
// BaseToggleFilterAreaを継承して共通ロジックを利用
// ----------------------------------------------------------------------
public class SetEvolutionStageArea : BaseToggleFilterArea<EvolutionStage>
{
    [Header("進化段階トグル")]
    [SerializeField] private Toggle basicToggle;       // たねポケモントグル
    [SerializeField] private Toggle stage1Toggle;      // 進化1トグル
    [SerializeField] private Toggle stage2Toggle;      // 進化2トグル
    
    // ----------------------------------------------------------------------
    // 基底クラスの抽象メソッドの実装
    // ----------------------------------------------------------------------
    
    protected override void InitializeToggleMappings()
    {
        toggleItemMap = new Dictionary<Toggle, EvolutionStage>();
        
        // null チェックを行ってからマッピングに追加
        AddToggleIfNotNull(basicToggle, EvolutionStage.たね);
        AddToggleIfNotNull(stage1Toggle, EvolutionStage.進化1);
        AddToggleIfNotNull(stage2Toggle, EvolutionStage.進化2);
    }

    private void AddToggleIfNotNull(Toggle toggle, EvolutionStage evolutionStage)
    {
        if (toggle != null)
        {
            toggleItemMap.Add(toggle, evolutionStage);
        }
        else
        {
            Debug.LogWarning($"Toggle for {evolutionStage} is null in SetEvolutionStageArea. Check Inspector assignments.");
        }
    }

    protected override void UpdateToggleVisualState(Toggle toggle, bool isOn)
    {
        if (toggle == null) return;

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
    // フィルター適用ロジック (IFilterAreaからオーバーライド)
    // ----------------------------------------------------------------------
    public override void ApplyFilterToModel(SearchModel model)
    {
        this.searchModel = model;

        if (this.searchModel != null)
        {
            // selectedItems は基底クラスで管理されている選択された項目のHashSetです
            this.searchModel.SetEvolutionStageFilter(new HashSet<EvolutionStage>(selectedItems));
        }
    }

    // ----------------------------------------------------------------------
    // 元の公開メソッドを維持（互換性のため）
    // ----------------------------------------------------------------------
    public HashSet<EvolutionStage> GetSelectedEvolutionStages()
    {
        return new HashSet<EvolutionStage>(selectedItems); // 基底クラスのselectedItemsを使用
    }

    public bool HasActiveFilters()
    {
        return selectedItems.Count > 0; // 基底クラスのselectedItemsを使用
    }
}