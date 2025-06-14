using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Enum;
using PokeDeck.Search.Filters.Base; // BaseToggleFilterAreaの名前空間をインポート

// ----------------------------------------------------------------------
// ポケモンタイプのフィルタリングを担当するPresenter
// BaseToggleFilterAreaを継承して共通ロジックを利用
// ----------------------------------------------------------------------
public class SetTypeArea : BaseToggleFilterArea<PokemonType> // BaseToggleFilterAreaを継承
{
    [Header("ポケモンタイプトグル")]
    [SerializeField] private Toggle grassToggle;       // 草タイプトグル
    [SerializeField] private Toggle fireToggle;        // 炎タイプトグル
    [SerializeField] private Toggle waterToggle;       // 水タイプトグル
    [SerializeField] private Toggle lightningToggle;   // 雷タイプトグル
    [SerializeField] private Toggle fightingToggle;    // 闘タイプトグル
    [SerializeField] private Toggle psychicToggle;     // 超タイプトグル
    [SerializeField] private Toggle darknessToggle;    // 悪タイプトグル
    [SerializeField] private Toggle steelToggle;       // 鋼タイプトグル
    [SerializeField] private Toggle dragonToggle;      // ドラゴンタイプトグル
    [SerializeField] private Toggle colorlessToggle;   // 無色タイプトグル
    
    // ----------------------------------------------------------------------
    // 基底クラスの抽象メソッドの実装: トグルとアイテムのマッピング
    // ----------------------------------------------------------------------
    protected override void InitializeToggleMappings()
    {
        toggleItemMap = new Dictionary<Toggle, PokemonType>();
        
        // null チェックを行ってからマッピングに追加
        AddToggleIfNotNull(grassToggle, PokemonType.草);
        AddToggleIfNotNull(fireToggle, PokemonType.炎);
        AddToggleIfNotNull(waterToggle, PokemonType.水);
        AddToggleIfNotNull(lightningToggle, PokemonType.雷);
        AddToggleIfNotNull(fightingToggle, PokemonType.闘);
        AddToggleIfNotNull(psychicToggle, PokemonType.超);
        AddToggleIfNotNull(darknessToggle, PokemonType.悪);
        AddToggleIfNotNull(steelToggle, PokemonType.鋼);
        AddToggleIfNotNull(dragonToggle, PokemonType.ドラゴン);
        AddToggleIfNotNull(colorlessToggle, PokemonType.無色);
    }

    private void AddToggleIfNotNull(Toggle toggle, PokemonType pokemonType)
    {
        if (toggle != null)
        {
            toggleItemMap.Add(toggle, pokemonType);
        }
        else
        {
            Debug.LogWarning($"Toggle for {pokemonType} is null in SetTypeArea. Check Inspector assignments.");
        }
    }

    // ----------------------------------------------------------------------
    // 基底クラスの抽象メソッドの実装: トグルの視覚的状態の更新
    // 元の UpdateToggleVisualState のロジックをここに記述します。
    // ----------------------------------------------------------------------
    protected override void UpdateToggleVisualState(Toggle toggle, bool isOn)
    {
        if (toggle == null) return;

        // SimpleToggleColorコンポーネントを取得して色を更新
        SimpleToggleColor colorComponent = toggle.GetComponent<SimpleToggleColor>();
        if (colorComponent != null)
        {
            colorComponent.UpdateColorState(isOn);
        }
        
        // TrueShadowToggleInsetコンポーネントを取得して影状態を更新
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
            this.searchModel.SetPokemonTypeFilter(new HashSet<PokemonType>(selectedItems));
        }
    }
    
    // ----------------------------------------------------------------------
    // 現在選択されているポケモンタイプのリストを取得 (元の機能を維持)
    // ----------------------------------------------------------------------
    public HashSet<PokemonType> GetSelectedTypes()
    {
        // selectedItems は基底クラスで管理されています
        return new HashSet<PokemonType>(selectedItems);
    }
    
    // ----------------------------------------------------------------------
    // 何かしらのポケモンタイプが選択されているかどうか (元の機能を維持)
    // ----------------------------------------------------------------------
    public bool HasActiveFilters()
    {
        // selectedItems は基底クラスで管理されています
        return selectedItems.Count > 0;
    }
    
    // ----------------------------------------------------------------------
    // フィルターのリセット (IFilterAreaからオーバーライド)
    // 基底クラスの ResetFilters() を呼び出すことで、selectedItems のクリアと
    // 各トグルの状態更新 (UpdateToggleVisualState の呼び出しを含む) が行われます。
    // ----------------------------------------------------------------------
    public override void ResetFilters()
    {
        base.ResetFilters(); 
        // OnFilterChanged?.Invoke(); は基底クラスの OnToggleValueChanged の中で
        // InvokeOnFilterChanged() として呼び出されるか、
        // ResetFilters の呼び出し元が一括で処理することを想定しています。
        // SearchView側のClearModelFiltersとResetUIの後に検索が実行されるため、
        // ここで明示的にイベントを発行する必要はありません。
    }
    
    // OnDestroy メソッドは、基底クラスの設計上、通常は具象クラスでの明示的なリスナー解除は不要です。
    // Toggleコンポーネントが破棄される際に、関連するリスナーも自動的にクリーンアップされます。
}