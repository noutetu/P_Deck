using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Enum;
using PokeDeck.Search.Filters.Base; // BaseToggleFilterAreaの名前空間をインポート

// ----------------------------------------------------------------------
// カードタイプのフィルタリングを担当するPresenter
// BaseToggleFilterAreaを継承して共通ロジックを利用
// ----------------------------------------------------------------------
public class SetCardTypeArea : BaseToggleFilterArea<CardType>
{
    [Header("カードタイプトグル")]
    [SerializeField] private Toggle nonEXToggle;
    [SerializeField] private Toggle exToggle;
    [SerializeField] private Toggle supportToggle;
    [SerializeField] private Toggle itemToggle;
    [SerializeField] private Toggle fossilToggle;
    [SerializeField] private Toggle pokemonToolToggle;
    
    // ----------------------------------------------------------------------
    // 基底クラスの抽象メソッドの実装
    // ----------------------------------------------------------------------
    
    protected override void InitializeToggleMappings()
    {
        toggleItemMap = new Dictionary<Toggle, CardType>();
        
        // null チェックを行ってから辞書に追加
        AddToggleIfNotNull(nonEXToggle, CardType.非EX);
        AddToggleIfNotNull(exToggle, CardType.EX);
        AddToggleIfNotNull(supportToggle, CardType.サポート);
        AddToggleIfNotNull(itemToggle, CardType.グッズ);
        AddToggleIfNotNull(fossilToggle, CardType.化石);
        AddToggleIfNotNull(pokemonToolToggle, CardType.ポケモンのどうぐ);
    }

    private void AddToggleIfNotNull(Toggle toggle, CardType cardType)
    {
        if (toggle != null)
        {
            toggleItemMap.Add(toggle, cardType);
        }
        else
        {
            Debug.LogWarning($"Toggle for {cardType} is null in SetCardTypeArea. Check Inspector assignments.");
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
            this.searchModel.SetCardTypeFilter(new HashSet<CardType>(selectedItems));
        }
    }

    // ----------------------------------------------------------------------
    // 元の公開メソッドを維持（互換性のため）
    // ----------------------------------------------------------------------
    public HashSet<CardType> GetSelectedCardTypes()
    {
        return new HashSet<CardType>(selectedItems); // 基底クラスのselectedItemsを使用
    }

    public bool HasActiveFilters()
    {
        return selectedItems.Count > 0; // 基底クラスのselectedItemsを使用
    }
}