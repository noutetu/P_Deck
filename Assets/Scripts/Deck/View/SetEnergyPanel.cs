using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ----------------------------------------------------------------------
// エネルギータイプ選択パネルを管理するクラス
// 最大2つまでのエネルギータイプを選択可能
// ----------------------------------------------------------------------
public class SetEnergyPanel : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // 定数管理
    // ----------------------------------------------------------------------
    private static class Constants
    {
        // UI設定
        public const int TOTAL_ENERGY_TOGGLE_SLOTS = 10;
        public const int AVAILABLE_ENERGY_TYPES_COUNT = 8;
        
        // エラーメッセージ
        public const string MAX_SELECTION_ERROR_FORMAT = "最大{0}つまでしか選択できません";
        
        // 配列インデックス
        public const int FIRST_ENERGY_INDEX = 0;
        public const int ENERGY_TYPE_ARRAY_MIN_INDEX = 0;
        
        // エネルギータイプ配列（定数として定義）
        public static readonly Enum.PokemonType[] AVAILABLE_ENERGY_TYPES = new Enum.PokemonType[]
        {
            Enum.PokemonType.草,
            Enum.PokemonType.炎,
            Enum.PokemonType.水,
            Enum.PokemonType.雷,
            Enum.PokemonType.超,
            Enum.PokemonType.闘,
            Enum.PokemonType.悪,
            Enum.PokemonType.鋼,
        };
    }

    // ----------------------------------------------------------------------
    // UI参照
    // ----------------------------------------------------------------------
    [Header("UI参照")]
    [SerializeField] private Toggle[] energyTypeToggles; // エネルギータイプのトグル（10種類）
    [SerializeField] private Image[] energyTypeImages; // エネルギータイプのアイコン画像

    // ----------------------------------------------------------------------
    // プライベート変数
    // ----------------------------------------------------------------------
    private HashSet<Enum.PokemonType> selectedTypes = new HashSet<Enum.PokemonType>(); // 現在選択されているタイプ
    private DeckModel deckModel; // 現在編集中のデッキモデル

    // ----------------------------------------------------------------------
    // エネルギータイプ画像リソース
    // ----------------------------------------------------------------------
    [Header("エネルギー画像")]
    [SerializeField] private Sprite grassEnergySprite; // 草エネルギー
    [SerializeField] private Sprite fireEnergySprite; // 炎エネルギー
    [SerializeField] private Sprite waterEnergySprite; // 水エネルギー
    [SerializeField] private Sprite lightningEnergySprite; // 雷エネルギー
    [SerializeField] private Sprite fightingEnergySprite; // 闘エネルギー
    [SerializeField] private Sprite psychicEnergySprite; // 超エネルギー
    [SerializeField] private Sprite darknessEnergySprite; // 悪エネルギー
    [SerializeField] private Sprite steelEnergySprite; // 鋼エネルギー

    // ----------------------------------------------------------------------
    // イベント
    // ----------------------------------------------------------------------
    public event Action<HashSet<Enum.PokemonType>> OnEnergyTypeSelected; // エネルギータイプが選択されたときのイベント

    // ======================================================================
    // Unity ライフサイクルメソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // 開始時
    // ----------------------------------------------------------------------
    private void Start()
    {
        // ボタンイベントの設定
        SetupButtonEvents();
    }

    // ======================================================================
    // UI 初期化メソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // トグルイベントの設定
    // ----------------------------------------------------------------------
    private void SetupButtonEvents()
    {
        if (energyTypeToggles == null) return;

        for (int i = Constants.FIRST_ENERGY_INDEX; i < energyTypeToggles.Length; i++)
        {
            SetupSingleToggle(i);
        }
    }

    // ----------------------------------------------------------------------
    // 個別のトグル設定
    // ----------------------------------------------------------------------
    private void SetupSingleToggle(int index)
    {
        if (energyTypeToggles[index] == null) return;

        bool isAvailable = index < Constants.AVAILABLE_ENERGY_TYPES.Length;
        energyTypeToggles[index].gameObject.SetActive(isAvailable);

        if (!isAvailable) return;

        // イベントリスナー設定
        int capturedIndex = index; // クロージャ対策
        energyTypeToggles[index].onValueChanged.AddListener(isOn => 
            OnEnergyTypeToggleChanged(capturedIndex, isOn));

        // エネルギー画像設定
        SetEnergyImage(index);
    }

    // ======================================================================
    // イベントハンドラー
    // ======================================================================

    // ----------------------------------------------------------------------
    // エネルギー画像設定
    // ----------------------------------------------------------------------
    private void SetEnergyImage(int index)
    {
        if (index >= energyTypeImages.Length || energyTypeImages[index] == null) return;
        
        energyTypeImages[index].sprite = GetEnergySprite(Constants.AVAILABLE_ENERGY_TYPES[index]);
    }

    // ----------------------------------------------------------------------
    // エネルギータイプトグルの値が変更されたとき
    // ----------------------------------------------------------------------
    private void OnEnergyTypeToggleChanged(int index, bool isOn)
    {
        if (!IsValidIndex(index)) return;

        Enum.PokemonType type = Constants.AVAILABLE_ENERGY_TYPES[index];

        if (isOn)
        {
            HandleToggleOn(type, index);
        }
        else
        {
            HandleToggleOff(type);
        }

        // 視覚的状態を更新
        UpdateToggleVisualState(energyTypeToggles[index], isOn);

        // 変更を通知
        OnEnergyTypeSelected?.Invoke(selectedTypes);
    }

    // ======================================================================
    // バリデーションメソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // インデックスの有効性をチェック
    // ----------------------------------------------------------------------
    private bool IsValidIndex(int index)
    {
        return index >= Constants.ENERGY_TYPE_ARRAY_MIN_INDEX && index < Constants.AVAILABLE_ENERGY_TYPES.Length;
    }

    // ======================================================================
    // トグル状態管理メソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // トグルON時の処理
    // ----------------------------------------------------------------------
    private void HandleToggleOn(Enum.PokemonType type, int index)
    {
        if (selectedTypes.Contains(type)) return;

        if (selectedTypes.Count < DeckModel.MAX_SELECTED_ENERGIES)
        {
            selectedTypes.Add(type);
        }
        else
        {
            ShowMaxSelectionError();
            ResetToggle(index);
        }
    }

    // ----------------------------------------------------------------------
    // トグルOFF時の処理
    // ----------------------------------------------------------------------
    private void HandleToggleOff(Enum.PokemonType type)
    {
        selectedTypes.Remove(type);
    }

    // ======================================================================
    // UI フィードバックメソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // 最大選択数エラー表示
    // ----------------------------------------------------------------------
    private void ShowMaxSelectionError()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowFailureFeedback(
                string.Format(Constants.MAX_SELECTION_ERROR_FORMAT, DeckModel.MAX_SELECTED_ENERGIES));
        }
    }

    // ======================================================================
    // 視覚的更新メソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // トグルをリセット（状態と視覚的状態の両方）
    // ----------------------------------------------------------------------
    private void ResetToggle(int index)
    {
        if (energyTypeToggles[index] != null)
        {
            Toggle toggle = energyTypeToggles[index];
            toggle.SetIsOnWithoutNotify(false);
            UpdateToggleVisualState(toggle, false);
        }
    }

    // ----------------------------------------------------------------------
    // トグルの視覚的状態を明示的に更新
    // ----------------------------------------------------------------------
    private void UpdateToggleVisualState(Toggle toggle, bool isOn)
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
    // トグル選択状態の更新
    // 指定されたトグルの選択状態を更新
    // イベントを発生させずに状態を変更し、視覚的状態も更新
    // index: 更新するトグルのインデックス
    // isSelected: 新しい選択状態
    // ----------------------------------------------------------------------
    private void UpdateToggleSelection(int index, bool isSelected)
    {
        // トグルの状態を更新（イベントを発生させない方法で）
        if (index >= Constants.ENERGY_TYPE_ARRAY_MIN_INDEX && index < energyTypeToggles.Length && energyTypeToggles[index] != null)
        {
            Toggle toggle = energyTypeToggles[index];

            // 現在の値と異なる場合のみ更新してイベントの無限ループを避ける
            if (toggle.isOn != isSelected)
            {
                // イベントを一時的に無効化するのが理想的だが、
                // ここでは単純に値のみを更新する
                toggle.SetIsOnWithoutNotify(isSelected);
                
                // 視覚的状態も明示的に更新
                UpdateToggleVisualState(toggle, isSelected);
            }
        }
    }

    // ======================================================================
    // パブリック API メソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // パネルを表示
    // エネルギー選択パネルを表示し、デッキの現在の選択状態を反映
    // デッキから選択済みエネルギータイプを読み込んでトグル状態を更新
    // deck: 表示するデッキモデル
    // ----------------------------------------------------------------------
    public void ShowPanel(DeckModel deck)
    {
        // デッキモデルの設定
        deckModel = deck;
        if (deckModel == null)
            return;

        // パネルを表示
        gameObject.SetActive(true);

        // 現在の選択状態をクリア
        selectedTypes.Clear();
        // トグルの選択状態を初期化
        for (int i = Constants.FIRST_ENERGY_INDEX; i < energyTypeToggles.Length; i++)
        {
            ResetToggle(i);
        }

        // デッキから選択済みエネルギーを読み込み
            foreach (var type in deckModel.SelectedEnergyTypes)
            {
                selectedTypes.Add(type);
            }

        // トグルの選択状態を更新
        UpdateAllToggleSelections();
    }

    // ----------------------------------------------------------------------
    // パネルを非表示
    // エネルギー選択パネルを非表示にする
    // パネルを閉じる際のクリーンアップ処理
    // ----------------------------------------------------------------------
    public void HidePanel()
    {
        // パネルを非表示
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------------
    // 現在選択されているエネルギータイプを取得
    // 現在選択されているエネルギータイプのセットを取得
    // 外部からの参照用にコピーを返す
    // returns: 選択されているエネルギータイプのハッシュセット
    // ----------------------------------------------------------------------
    public HashSet<Enum.PokemonType> GetSelectedTypes()
    {
        // 現在選択されているエネルギータイプのコピーを返す
        return new HashSet<Enum.PokemonType>(selectedTypes);
    }

    // ======================================================================
    // ヘルパーメソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // 全てのトグルの選択状態を更新
    // 全てのエネルギータイプトグルの選択状態を現在の選択状態に合わせて更新
    // パネル表示時やデッキデータ読み込み時に使用
    // ----------------------------------------------------------------------
    private void UpdateAllToggleSelections()
    {
        // 全てのトグルの選択状態を更新
        for (int i = Constants.FIRST_ENERGY_INDEX; i < Constants.AVAILABLE_ENERGY_TYPES.Length && i < energyTypeToggles.Length; i++)
        {
            Enum.PokemonType type = Constants.AVAILABLE_ENERGY_TYPES[i];
            UpdateToggleSelection(i, selectedTypes.Contains(type));
        }
    }

    // ----------------------------------------------------------------------
    // エネルギータイプに対応するスプライトを取得
    // 指定されたエネルギータイプに対応するスプライトを取得
    // エネルギーアイコンの表示やボタン画像の設定に使用
    // type: スプライトを取得するエネルギータイプ
    // returns: 対応するスプライト、存在しない場合はnull
    // ----------------------------------------------------------------------
    public Sprite GetEnergySprite(Enum.PokemonType type)
    {
        switch (type)
        {
            case Enum.PokemonType.草: return grassEnergySprite;
            case Enum.PokemonType.炎: return fireEnergySprite;
            case Enum.PokemonType.水: return waterEnergySprite;
            case Enum.PokemonType.雷: return lightningEnergySprite;
            case Enum.PokemonType.闘: return fightingEnergySprite;
            case Enum.PokemonType.超: return psychicEnergySprite;
            case Enum.PokemonType.悪: return darknessEnergySprite;
            case Enum.PokemonType.鋼: return steelEnergySprite;
            default: return null;
        }
    }
}