using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Enum;
using PokeDeck.Search.Filters.Base; // BaseToggleFilterAreaの名前空間をインポート

// ----------------------------------------------------------------------
// カードパックのフィルタリングを担当するPresenter
// BaseToggleFilterAreaを継承して共通ロジックを利用
// ----------------------------------------------------------------------
public class SetCardPackArea : BaseToggleFilterArea<CardPack>
{
    [Header("カードパックトグル")]
    [SerializeField] private Toggle saikyo_Toggle;          // 最強の遺伝子トグル
    [SerializeField] private Toggle maboroshi_Toggle;       // 幻のいる島トグル
    [SerializeField] private Toggle jikuu_Toggle;           // 時空の激闘トグル
    [SerializeField] private Toggle choukoku_Toggle;        // 超克の光トグル
    [SerializeField] private Toggle shiningHigh_Toggle;     // シャイニングハイトグル
    [SerializeField] private Toggle souten_Toggle;          // 双天の守護者トグル
    [SerializeField] private Toggle ijigen_Toggle;          // 異次元クライシストグル
    [SerializeField] private Toggle promo_Toggle;           // PROMOトグル
    
    // ----------------------------------------------------------------------
    // 基底クラスの抽象メソッドの実装
    // ----------------------------------------------------------------------
    
    protected override void InitializeToggleMappings()
    {
        toggleItemMap = new Dictionary<Toggle, CardPack>();
        
        // null チェックを行ってからマッピングに追加
        AddToggleIfNotNull(saikyo_Toggle, CardPack.最強の遺伝子);
        AddToggleIfNotNull(maboroshi_Toggle, CardPack.幻のいる島);
        AddToggleIfNotNull(jikuu_Toggle, CardPack.時空の激闘);
        AddToggleIfNotNull(choukoku_Toggle, CardPack.超克の光);
        AddToggleIfNotNull(shiningHigh_Toggle, CardPack.シャイニングハイ);
        AddToggleIfNotNull(souten_Toggle, CardPack.双天の守護者);
        AddToggleIfNotNull(ijigen_Toggle, CardPack.異次元クライシス);
        AddToggleIfNotNull(promo_Toggle, CardPack.PROMO);
    }

    private void AddToggleIfNotNull(Toggle toggle, CardPack cardPack)
    {
        if (toggle != null)
        {
            toggleItemMap.Add(toggle, cardPack);
        }
        else
        {
            Debug.LogWarning($"Toggle for {cardPack} is null in SetCardPackArea. Check Inspector assignments.");
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
            this.searchModel.SetCardPackFilter(new HashSet<CardPack>(selectedItems));
        }
    }

    // ----------------------------------------------------------------------
    // 元の公開メソッドを維持（互換性のため）
    // ----------------------------------------------------------------------
    public HashSet<CardPack> GetSelectedCardPacks()
    {
        return new HashSet<CardPack>(selectedItems); // 基底クラスのselectedItemsを使用
    }

    public bool HasActiveFilters()
    {
        return selectedItems.Count > 0; // 基底クラスのselectedItemsを使用
    }
}