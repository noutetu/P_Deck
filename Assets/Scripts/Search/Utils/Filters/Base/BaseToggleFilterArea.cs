using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PokeDeck.Search.Filters.Base
{
    // ----------------------------------------------------------------------
    // トグルスイッチを使用して複数の項目を選択するフィルターエリアの基底クラス。
    // @typeparam TItem フィルター項目を表すEnum型を想定。
    // ----------------------------------------------------------------------
    public abstract class BaseToggleFilterArea<TItem> : BaseFilterArea where TItem : struct, System.IConvertible
    {
        // ----------------------------------------------------------------------
        // Fields
        // ----------------------------------------------------------------------
        
        // 選択されたフィルター項目を保持します。
        protected HashSet<TItem> selectedItems = new HashSet<TItem>();

        // 各トグルとフィルター項目のマッピング。
        // キーがトグル、値が対応するフィルター項目。
        protected Dictionary<Toggle, TItem> toggleItemMap = new Dictionary<Toggle, TItem>();

        // SearchModelへの参照（基底クラスで管理）
        protected SearchModel searchModel;

        // ----------------------------------------------------------------------
        // Lifecycle Methods
        // ----------------------------------------------------------------------
        // ----------------------------------------------------------------------
        // 基底クラスのStart処理を呼び出し、初期化処理を実行します。
        // ----------------------------------------------------------------------
        protected override void Start()
        {
            // 基底クラスのStart処理を呼び出し
            base.Start();
            // 初期化処理を実行
            initialize();
        }

        // ----------------------------------------------------------------------
        // Initialization
        // ----------------------------------------------------------------------
        // ----------------------------------------------------------------------
        // 初期化処理を行います。
        // トグルのマッピングを初期化し、各トグルにイベントリスナーを設定します。
        // ----------------------------------------------------------------------
        private void initialize()
        {
            // 具象クラスで定義されたトグルとアイテムのマッピングを初期化
            InitializeToggleMappings();

            // マッピングが空の場合、警告ログを出力
            if (toggleItemMap.Count == 0)
            {
                Debug.LogWarning($"No toggle mappings initialized for {this.GetType().Name}. Ensure InitializeToggleMappings is implemented and populates toggleItemMap.");
                return; // マッピングがない場合は以降の処理を行わない
            }

            // 各トグルに対して初期設定とイベントリスナーの登録を行う
            foreach (var toggleEntry in toggleItemMap)
            {
                setupToggle(toggleEntry.Key, toggleEntry.Value);
            }
        }

        // ----------------------------------------------------------------------
        // 指定されたトグルの初期設定とイベントリスナーの登録を行います。
        // @param toggle 設定対象のトグル。
        // @param item トグルに対応するフィルター項目。
        // ----------------------------------------------------------------------
        private void setupToggle(Toggle toggle, TItem item)
        {
            // トグルがnullの場合、エラーログを出力してスキップ
            if (toggle == null)
            {
                Debug.LogError($"Null toggle found in toggleItemMap for item {item} in {this.GetType().Name}.");
                return;
            }

            // トグルの初期状態をselectedItemsに反映
            if (toggle.isOn)
            {
                selectedItems.Add(item);
            }
            // トグルの初期の視覚的状態を更新
            UpdateToggleVisualState(toggle, toggle.isOn);
            // トグルの値変更イベントにリスナーを登録
            toggle.onValueChanged.AddListener(isOn => OnToggleValueChanged(toggle, item, isOn));
        }

        // ----------------------------------------------------------------------
        // 具象クラスで、toggleItemMapにトグルと対応するTItemをマッピングします。
        // 例: toggleItemMap.Add(grassToggle, PokemonType.Grass);
        // ----------------------------------------------------------------------
        protected abstract void InitializeToggleMappings();

        // ----------------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------------
        // ----------------------------------------------------------------------
        // 現在のフィルター設定をSearchModelに適用します。
        // 基本的な実装では、searchModelフィールドを設定するのみです。
        // 具象クラスで具体的なフィルター適用ロジックを実装してください。
        // ----------------------------------------------------------------------
        public override void ApplyFilterToModel(SearchModel model)
        {
            this.searchModel = model;
        }

        // ----------------------------------------------------------------------
        // フィルター設定をリセットします。
        // 選択項目をクリアし、すべてのトグルをオフ状態にします。
        // ----------------------------------------------------------------------
        public override void ResetFilters()
        {
            // 選択項目をクリア
            selectedItems.Clear();
            // すべてのトグルをオフ状態にする
            foreach (var toggleEntry in toggleItemMap)
            {
                Toggle toggle = toggleEntry.Key;
                if (toggle != null)
                {
                    // toggle.isOn を false に設定することで、
                    // OnToggleValueChanged リスナーがトリガーされ、
                    // UpdateToggleVisualState も呼び出される
                    toggle.isOn = false;
                }
            }
            // ResetFilters時は親が一括で変更を通知することが多いため、
            // ここではInvokeOnFilterChangedを意図的に呼ばない。
            // 必要であれば、呼び出し側や具象クラスで対応する。
        }

        // ----------------------------------------------------------------------
        // Event Handlers
        // ----------------------------------------------------------------------
        // ----------------------------------------------------------------------
        // トグルの値が変更されたときに呼び出されます。
        // 選択状態を更新し、フィルター変更イベントを発行します。
        // @param toggle 状態が変更されたトグル。
        // @param item トグルに対応するフィルター項目。
        // @param isOn トグルがオンになったかどうか。
        // ----------------------------------------------------------------------
        protected virtual void OnToggleValueChanged(Toggle toggle, TItem item, bool isOn)
        {
            // トグルがオンになった場合、選択項目に追加
            if (isOn)
            {
                selectedItems.Add(item);
            }
            // トグルがオフになった場合、選択項目から削除
            else
            {
                selectedItems.Remove(item);
            }
            // トグルの視覚的状態を更新
            UpdateToggleVisualState(toggle, isOn);
            // フィルターが変更されたことを通知
            InvokeOnFilterChanged();
        }

        // ----------------------------------------------------------------------
        // Helper Methods
        // ----------------------------------------------------------------------
        // ----------------------------------------------------------------------
        // トグルの視覚的状態を更新します。
        // 具象クラスで具体的な表示ロジックを実装します (例: 色、影の変更)。
        // @param toggle 対象のトグル。
        // @param isOn トグルがオンかどうか。
        // ----------------------------------------------------------------------
        protected abstract void UpdateToggleVisualState(Toggle toggle, bool isOn);
    }
}
