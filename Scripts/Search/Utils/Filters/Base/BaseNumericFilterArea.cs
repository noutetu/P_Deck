using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PokeDeck.Search.Filters.Base
{
    // ----------------------------------------------------------------------
    // 数値とそれに対する比較条件を指定するフィルターエリアの基底クラス。
    // @typeparam TComparisonEnum 比較条件を表すEnum型 (例: LessOrEqual, Equal, GreaterOrEqual)。
    // ----------------------------------------------------------------------
    public abstract class BaseNumericFilterArea<TComparisonEnum> : BaseFilterArea where TComparisonEnum : struct, System.IConvertible
    {
        // ======================================================================
        // Constants / 定数
        // ======================================================================
        private static class Constants
        {
            public const int DROPDOWN_DEFAULT_INDEX = 0;
        }
        
        // ======================================================================
        // Fields / フィールド
        // ======================================================================
        [Header("Value Dropdown")]
        [SerializeField] protected Dropdown valueDropdown;

        protected TComparisonEnum selectedComparisonType;
        protected int selectedValue; // ドロップダウンで選択された数値 (パース後)
        protected string selectedDropdownText; // ドロップダウンで選択された生のテキスト
        protected SearchModel searchModel; // SearchModelへの参照（基底クラスで管理）
        protected Dictionary<Toggle, TComparisonEnum> comparisonToggleMap = new Dictionary<Toggle, TComparisonEnum>();
        protected TComparisonEnum defaultComparisonType; // 比較タイプが指定されていない場合のデフォルト値

        // ======================================================================
        // Lifecycle Methods / ライフサイクル
        // ======================================================================
        protected virtual void Awake()
        {
            // Unity lifecycle method - intentionally minimal
        }
        
        protected virtual void OnEnable()
        {
            tryEmergencyDropdownInitialization();
        }
        
        protected override void Start()
        {
            base.Start();
            initialize();
        }

        // ======================================================================
        // Initialization / 初期化メソッド
        // ======================================================================
        
        // ----------------------------------------------------------------------
        // メイン初期化処理
        // ----------------------------------------------------------------------
        private void initialize()
        {
            try
            {
                setupDefaultComparisonType();
                initializeToggleSystem();
                initializeDropdownSystem();
                setInitialVisualState();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception in Initialize for {this.GetType().Name}: {ex.Message}");
                Debug.LogException(ex);
                throw;
            }
        }
        
        // ----------------------------------------------------------------------
        // デフォルト比較タイプの設定
        // ----------------------------------------------------------------------
        private void setupDefaultComparisonType()
        {
            defaultComparisonType = GetDefaultComparisonType(); 
            selectedComparisonType = defaultComparisonType;
        }
        
        // ----------------------------------------------------------------------
        // トグルシステムの初期化
        // ----------------------------------------------------------------------
        private void initializeToggleSystem()
        {
            InitializeComparisonToggleMappings(); 
            initializeComparisonToggles();
            performAdditionalToggleSetup(); // 具象クラスでの追加設定用
        }
        
        // ----------------------------------------------------------------------
        // 具象クラスでのトグル追加設定（オーバーライド可能）
        // ----------------------------------------------------------------------
        protected virtual void performAdditionalToggleSetup()
        {
            // デフォルトでは何もしない
            // 具象クラスでToggleGroupの設定など追加処理を実装可能
        }
        
        // ----------------------------------------------------------------------
        // ドロップダウンシステムの初期化
        // ----------------------------------------------------------------------
        private void initializeDropdownSystem()
        {
            if (valueDropdown != null)
            {
                initializeDropdown();
            }
            else
            {
                Debug.LogError($"valueDropdown is null during Initialize for {this.GetType().Name}. This may indicate a serialization issue.");
            }
        }
        
        // ----------------------------------------------------------------------
        // 緊急時のドロップダウン初期化試行
        // ----------------------------------------------------------------------
        private void tryEmergencyDropdownInitialization()
        {
            if (valueDropdown != null && valueDropdown.options.Count == Constants.DROPDOWN_DEFAULT_INDEX)
            {
                PopulateDropdownOptions();
            }
        }
        
        // ----------------------------------------------------------------------
        // 比較トグルの初期化とイベントリスナー設定
        // ----------------------------------------------------------------------
        private void initializeComparisonToggles()
        {
            if (!validateComparisonToggleMap()) return;
            
            setupToggleEventListeners();
        }
        
        // ----------------------------------------------------------------------
        // 比較トグルマップの検証
        // ----------------------------------------------------------------------
        private bool validateComparisonToggleMap()
        {
            if (comparisonToggleMap.Count == Constants.DROPDOWN_DEFAULT_INDEX)
            {
                Debug.LogWarning($"No comparison toggle mappings initialized for {this.GetType().Name}. Ensure InitializeComparisonToggleMappings is implemented.");
                return false;
            }
            return true;
        }
        
        // ----------------------------------------------------------------------
        // トグルイベントリスナーの設定
        // ----------------------------------------------------------------------
        private void setupToggleEventListeners()
        {
            foreach (KeyValuePair<Toggle, TComparisonEnum> entry in comparisonToggleMap)
            {
                Toggle toggle = entry.Key;
                TComparisonEnum type = entry.Value;
                
                if (toggle != null)
                {
                    toggle.onValueChanged.AddListener(isOn => OnComparisonToggleChanged(toggle, type, isOn));
                }
                else
                {
                    Debug.LogError($"Null toggle found in comparisonToggleMap for type {type} in {this.GetType().Name}");
                }
            }
        }
        
        // ----------------------------------------------------------------------
        // ドロップダウンの初期化とイベントリスナー設定
        // ----------------------------------------------------------------------
        private void initializeDropdown()
        {
            if (valueDropdown == null)
            {
                Debug.LogWarning($"valueDropdown is null for {this.GetType().Name}. Cannot initialize dropdown options.");
                return;
            }
            
            setupDropdownOptions();
            setupDropdownEventListener();
            setInitialDropdownValues();
        }
        
        // ----------------------------------------------------------------------
        // ドロップダウンの選択肢設定
        // ----------------------------------------------------------------------
        private void setupDropdownOptions()
        {
            PopulateDropdownOptions();
        }
        
        // ----------------------------------------------------------------------
        // ドロップダウンのイベントリスナー設定
        // ----------------------------------------------------------------------
        private void setupDropdownEventListener()
        {
            valueDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
        
        // ----------------------------------------------------------------------
        // ドロップダウンの初期値設定
        // ----------------------------------------------------------------------
        private void setInitialDropdownValues()
        {
            if (valueDropdown.options.Count > Constants.DROPDOWN_DEFAULT_INDEX)
            {
                selectedDropdownText = valueDropdown.options[valueDropdown.value].text;
                TryParseDropdownValue(selectedDropdownText, out selectedValue);
            }
            else
            {
                selectedDropdownText = GetDropdownDefaultText();
                selectedValue = GetDefaultNumericValue();
            }
        }

        // ----------------------------------------------------------------------
        // フィルターの初期視覚状態を設定
        // ----------------------------------------------------------------------
        private void setInitialVisualState()
        {
            updateAllToggleVisualStates();
        }
        
        // ----------------------------------------------------------------------
        // 全トグルの視覚状態を更新
        // ----------------------------------------------------------------------
        private void updateAllToggleVisualStates()
        {
            foreach (KeyValuePair<Toggle, TComparisonEnum> entry in comparisonToggleMap)
            {
                if (entry.Key != null)
                {
                    UpdateComparisonToggleVisualState(entry.Key, entry.Key.isOn);
                }
            }
        }

        // ======================================================================
        // Event Handlers / イベントハンドラー
        // ======================================================================
        
        // ----------------------------------------------------------------------
        // 比較トグルの値変更時の処理
        // @param activeToggle 状態が変更されたトグル
        // @param type トグルに対応する比較タイプ
        // @param isOn トグルがオンになったかどうか
        // ----------------------------------------------------------------------
        protected virtual void OnComparisonToggleChanged(Toggle activeToggle, TComparisonEnum type, bool isOn)
        {
            if (isOn)
            {
                handleToggleActivation(activeToggle, type);
            }
            else
            {
                handleToggleDeactivation(activeToggle);
            }

            tryInvokeFilterChangedEvent();
        }
        
        // ----------------------------------------------------------------------
        // トグル有効化の処理
        // ----------------------------------------------------------------------
        private void handleToggleActivation(Toggle activeToggle, TComparisonEnum type)
        {
            selectedComparisonType = type;
            deactivateOtherToggles(activeToggle);
            UpdateComparisonToggleVisualState(activeToggle, true);
        }
        
        // ----------------------------------------------------------------------
        // 他のトグルを無効化
        // ----------------------------------------------------------------------
        private void deactivateOtherToggles(Toggle activeToggle)
        {
            foreach (KeyValuePair<Toggle, TComparisonEnum> entry in comparisonToggleMap)
            {
                if (entry.Key != activeToggle && entry.Key.isOn)
                {
                    entry.Key.SetIsOnWithoutNotify(false);
                    UpdateComparisonToggleVisualState(entry.Key, false);
                }
            }
        }
        
        // ----------------------------------------------------------------------
        // トグル無効化の処理
        // ----------------------------------------------------------------------
        private void handleToggleDeactivation(Toggle activeToggle)
        {
            bool anyOtherToggleOn = checkIfAnyToggleIsOn();
            
            if (!anyOtherToggleOn)
            {
                selectedComparisonType = defaultComparisonType;
            }
            
            UpdateComparisonToggleVisualState(activeToggle, false);
        }
        
        // ----------------------------------------------------------------------
        // いずれかのトグルがオンか確認
        // ----------------------------------------------------------------------
        private bool checkIfAnyToggleIsOn()
        {
            foreach (KeyValuePair<Toggle, TComparisonEnum> entry in comparisonToggleMap)
            {
                if (entry.Key.isOn)
                {
                    return true;
                }
            }
            return false;
        }
        
        // ----------------------------------------------------------------------
        // フィルター変更イベントの発火を試行
        // ----------------------------------------------------------------------
        private void tryInvokeFilterChangedEvent()
        {
            if (IsFilterEffectivelyActive())
            {
                InvokeOnFilterChanged();
            }
        }

        // ----------------------------------------------------------------------
        // ドロップダウンの値変更時の処理
        // @param index 選択されたドロップダウンのインデックス
        // ----------------------------------------------------------------------
        protected virtual void OnDropdownValueChanged(int index)
        {
            if (!validateDropdownIndex(index)) return;

            updateSelectedDropdownValues(index);
            handleDropdownValueSelection();
        }
        
        // ----------------------------------------------------------------------
        // ドロップダウンインデックスの検証
        // ----------------------------------------------------------------------
        private bool validateDropdownIndex(int index)
        {
            return valueDropdown != null && index >= Constants.DROPDOWN_DEFAULT_INDEX && index < valueDropdown.options.Count;
        }
        
        // ----------------------------------------------------------------------
        // 選択されたドロップダウン値の更新
        // ----------------------------------------------------------------------
        private void updateSelectedDropdownValues(int index)
        {
            selectedDropdownText = valueDropdown.options[index].text;
            TryParseDropdownValue(selectedDropdownText, out selectedValue);
        }
        
        // ----------------------------------------------------------------------
        // ドロップダウン値選択の処理
        // ----------------------------------------------------------------------
        private void handleDropdownValueSelection()
        {
            bool parsedSuccessfully = TryParseDropdownValue(selectedDropdownText, out selectedValue);
            
            if (shouldActivateDefaultToggle(parsedSuccessfully))
            {
                activateDefaultToggleForDropdown();
                return;
            }
            
            if (!parsedSuccessfully || selectedDropdownText == GetDropdownDefaultText())
            {
                resetToDefaultValue();
            }
            
            tryInvokeFilterChangedEvent();
        }
        
        // ----------------------------------------------------------------------
        // デフォルトトグルを有効化すべきかの判定
        // ----------------------------------------------------------------------
        private bool shouldActivateDefaultToggle(bool parsedSuccessfully)
        {
            return parsedSuccessfully && 
                   selectedDropdownText != GetDropdownDefaultText() && 
                   !checkIfAnyToggleIsOn();
        }
        
        // ----------------------------------------------------------------------
        // ドロップダウン用のデフォルトトグルを有効化
        // ----------------------------------------------------------------------
        private void activateDefaultToggleForDropdown()
        {
            Toggle defaultToggleForInteraction = GetDefaultComparisonToggleForDropdownInteraction();
            if (defaultToggleForInteraction != null && !defaultToggleForInteraction.isOn)
            {
                defaultToggleForInteraction.isOn = true;
                StartCoroutine(delayedFilterChangeEvent());
            }
        }
        
        // ----------------------------------------------------------------------
        // デフォルト値にリセット
        // ----------------------------------------------------------------------
        private void resetToDefaultValue()
        {
            selectedValue = GetDefaultNumericValue();
        }
        
        // ----------------------------------------------------------------------
        // フィルター変更イベントを遅延発火するコルーチン
        // ----------------------------------------------------------------------
        private System.Collections.IEnumerator delayedFilterChangeEvent()
        {
            yield return null; // 1フレーム待機
            tryInvokeFilterChangedEvent();
        }
        
        // ======================================================================
        // Abstract Methods (for concrete class implementation)
        // ======================================================================
        protected abstract void InitializeComparisonToggleMappings();
        protected abstract void PopulateDropdownOptions();
        protected abstract TComparisonEnum GetDefaultComparisonType();
        protected abstract string GetDropdownDefaultText();
        protected abstract int GetDefaultNumericValue();
        protected abstract bool TryParseDropdownValue(string text, out int value);
        protected abstract Toggle GetDefaultComparisonToggleForDropdownInteraction();
        protected abstract void UpdateComparisonToggleVisualState(Toggle toggle, bool isOn);

        // ======================================================================
        // Public API / 公開メソッド
        // ======================================================================
        
        // ----------------------------------------------------------------------
        // 現在のフィルター設定をSearchModelに適用します。
        // @param model 適用対象のSearchModel
        // ----------------------------------------------------------------------
        public override void ApplyFilterToModel(SearchModel model)
        {
            this.searchModel = model;
        }

        // ----------------------------------------------------------------------
        // フィルター設定をリセットします。
        // ----------------------------------------------------------------------
        public override void ResetFilters()
        {
            resetComparisonType();
            resetDropdownValues();
            resetAllToggles();
        }
        
        // ----------------------------------------------------------------------
        // 手動で初期化を実行するメソッド（テスト用）
        // ----------------------------------------------------------------------
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        public void ManuallyInitialize()
        {
            try
            {
                initialize();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Exception in ManuallyInitialize for {this.GetType().Name}: {ex.Message}");
                Debug.LogException(ex);
                throw;
            }
        }
#endif
        
        // ----------------------------------------------------------------------
        // valueDropdownの状態を確認するメソッド（テスト用）
        // ----------------------------------------------------------------------
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        public void CheckDropdownState()
        {
            Debug.Log($"CheckDropdownState for {this.GetType().Name}:");
            Debug.Log($"  valueDropdown is null: {valueDropdown == null}");
            if (valueDropdown != null)
            {
                Debug.Log($"  valueDropdown.options.Count: {valueDropdown.options.Count}");
                Debug.Log($"  valueDropdown.value: {valueDropdown.value}");
            }
        }
#endif

        // ======================================================================
        // Helper Methods / ヘルパーメソッド
        // ======================================================================
        
        // ----------------------------------------------------------------------
        // 比較タイプのリセット
        // ----------------------------------------------------------------------
        private void resetComparisonType()
        {
            selectedComparisonType = defaultComparisonType;
        }
        
        // ----------------------------------------------------------------------
        // ドロップダウン値のリセット
        // ----------------------------------------------------------------------
        private void resetDropdownValues()
        {
            if (valueDropdown != null && valueDropdown.options.Count > Constants.DROPDOWN_DEFAULT_INDEX)
            {
                valueDropdown.value = Constants.DROPDOWN_DEFAULT_INDEX; 
                selectedDropdownText = valueDropdown.options[Constants.DROPDOWN_DEFAULT_INDEX].text; 
                TryParseDropdownValue(selectedDropdownText, out selectedValue);
            }
            else
            {
                selectedDropdownText = GetDropdownDefaultText();
                selectedValue = GetDefaultNumericValue();
            }
        }
        
        // ----------------------------------------------------------------------
        // 全トグルのリセット
        // ----------------------------------------------------------------------
        private void resetAllToggles()
        {
            foreach (KeyValuePair<Toggle, TComparisonEnum> entry in comparisonToggleMap)
            {
                if (entry.Key != null)
                {
                    entry.Key.isOn = false;
                }
            }
        }

        // ======================================================================
        // Utility / ユーティリティ
        // ======================================================================
        
        // ----------------------------------------------------------------------
        // フィルターが実際に有効な条件を持っているか判定
        // @return フィルターが有効な場合はtrue、そうでない場合はfalse
        // ----------------------------------------------------------------------
        protected virtual bool IsFilterEffectivelyActive()
        {
            bool comparisonIsSet = isComparisonTypeChanged();
            bool valueIsMeaningful = isDropdownValueMeaningful();
            bool anyToggleOn = checkIfAnyToggleIsOn();
            
            return (anyToggleOn && valueIsMeaningful) || (comparisonIsSet && valueIsMeaningful);
        }
        
        // ----------------------------------------------------------------------
        // 比較タイプがデフォルトから変更されているか確認
        // ----------------------------------------------------------------------
        private bool isComparisonTypeChanged()
        {
            return !EqualityComparer<TComparisonEnum>.Default.Equals(selectedComparisonType, defaultComparisonType);
        }
        
        // ----------------------------------------------------------------------
        // ドロップダウンの値が意味を持つか確認
        // ----------------------------------------------------------------------
        private bool isDropdownValueMeaningful()
        {
            return selectedDropdownText != GetDropdownDefaultText() && 
                   TryParseDropdownValue(selectedDropdownText, out _);
        }
    }
}
