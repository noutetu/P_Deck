using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// ----------------------------------------------------------------------
// サンプルデッキ一覧パネルを管理するクラス
// DeckManagerからサンプルデッキを取得し、表示する
// ----------------------------------------------------------------------
public class SampleDeckPanel : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // 定数管理
    // ----------------------------------------------------------------------
    private static class Constants
    {
        public const string LOADING_MESSAGE_FORMAT = "サンプルデッキ '{0}' を読み込み中...";
        public const string DISPLAY_MESSAGE = "デッキを表示中...";
        public const string SUCCESS_MESSAGE_FORMAT = "サンプルデッキ '{0}' を選択しました";
        public const string SELECTION_ERROR_MESSAGE = "サンプルデッキの選択に失敗しました";
        public const string DISPLAY_ERROR_MESSAGE = "デッキ表示中にエラーが発生しました";
    }

    // ----------------------------------------------------------------------
    // フィールド変数
    // ----------------------------------------------------------------------
    [SerializeField] private Transform contentContainer;    // デッキ一覧のコンテナ
    [SerializeField] private GameObject deckDetailPrefab;   // デッキ詳細アイテムのプレハブ
    [SerializeField] private GameObject deckListPanel;       // デッキパネル
    [SerializeField] private DeckView deckView;       // デッキビュー
    [SerializeField] private Button closeButton;       // 閉じるボタン
    
    private List<GameObject> deckItems = new List<GameObject>();    // デッキアイテムのリスト
    
    // ----------------------------------------------------------------------
    // ライフサイクルメソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    // パネルが有効になったときの処理
    // @description パネルが表示されるたびにデッキリストを更新
    // ----------------------------------------------------------------------
    private void OnEnable()
    {
        RefreshDeckList();
    }
    
    // ----------------------------------------------------------------------
    // 初期化処理
    // @description 閉じるボタンのイベント設定を行う
    // ----------------------------------------------------------------------
    private void Start()
    {
        SetupCloseButton();
    }
    
    // ----------------------------------------------------------------------
    // 初期化・セットアップメソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    // 閉じるボタンの設定
    // @description 閉じるボタンのクリックイベントを設定
    // ----------------------------------------------------------------------
    private void SetupCloseButton()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }
    
    // ----------------------------------------------------------------------
    // 公開メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    // デッキ一覧を最新の状態に更新
    // @description 既存のデッキアイテムをクリアし、サンプルデッキを取得して表示
    // ----------------------------------------------------------------------
    public void RefreshDeckList()
    {
        ClearDeckItems();
        
        if (!EnsureDeckManagerExists())
        {
            return;
        }
        
        DisplaySampleDecks();
    }
    
    // ----------------------------------------------------------------------
    // イベントハンドラー
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    // 閉じるボタンクリック時の処理
    // @description パネルを非表示にし、デッキリストパネルを表示
    // ----------------------------------------------------------------------
    private void OnCloseButtonClicked()
    {
        gameObject.SetActive(false);
        ShowDeckListPanel();
    }
    
    // ----------------------------------------------------------------------
    // デッキ選択時の処理
    // @param deckName 選択されたデッキ名
    // @description 指定されたサンプルデッキを選択し、表示
    // ----------------------------------------------------------------------
    private async void OnDeckSelected(string deckName)
    {
        if (!EnsureDeckManagerExists())
        {
            return;
        }

        ShowLoadingMessage(deckName);

        try
        {
            if (!SelectSampleDeck(deckName))
            {
                ShowSelectionErrorMessage();
                return;
            }

            await DisplaySelectedDeck(deckName);
            ShowSuccessMessage(deckName);
            CloseAndShowDeckView();
        }
        catch (System.Exception)
        {
            ShowDisplayErrorMessage();
        }
    }
    
    // ----------------------------------------------------------------------
    // デッキリスト管理メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    // 既存のデッキアイテムをすべて削除
    // @description デッキアイテムのリストをクリアし、GameObjectを破棄
    // ----------------------------------------------------------------------
    private void ClearDeckItems()
    {
        foreach (var item in deckItems)
        {
            Destroy(item);
        }
        
        deckItems.Clear();
    }
    
    // ----------------------------------------------------------------------
    // サンプルデッキの表示処理
    // @description DeckManagerからサンプルデッキを取得し、アイテムを生成
    // ----------------------------------------------------------------------
    private void DisplaySampleDecks()
    {
        var sampleDecks = DeckManager.Instance.SampleDecks;
        
        foreach (var deck in sampleDecks)
        {
            if (deck != null)
            {
                CreateDeckItem(deck);
            }
        }
    }
    
    // ----------------------------------------------------------------------
    // デッキアイテムを生成
    // @param deck 生成するデッキのモデル
    // @description デッキアイテムのプレハブを生成し、設定を行う
    // ----------------------------------------------------------------------
    private void CreateDeckItem(DeckModel deck)
    {
        if (!ValidateCreateDeckItemParameters(deck))
        {
            return;
        }
            
        GameObject deckItem = InstantiateDeckItem();
        SetupDeckItemComponent(deckItem, deck);
    }
    
    // ----------------------------------------------------------------------
    // デッキ選択処理
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    // サンプルデッキの選択処理
    // @param deckName 選択するデッキ名
    // @returns 選択が成功したかどうか
    // @description DeckManagerでサンプルデッキを選択
    // ----------------------------------------------------------------------
    private bool SelectSampleDeck(string deckName)
    {
        return DeckManager.Instance.SelectDeck(deckName);
    }
    
    // ----------------------------------------------------------------------
    // 選択されたデッキの表示処理
    // @param deckName 表示するデッキ名
    // @description DeckViewで選択されたデッキを表示
    // ----------------------------------------------------------------------
    private async System.Threading.Tasks.Task DisplaySelectedDeck(string deckName)
    {
        if (deckView != null)
        {
            ShowDisplayMessage();
            await deckView.DisplayDeck(DeckManager.Instance.CurrentDeck);
        }
    }
    
    // ----------------------------------------------------------------------
    // 設定・ヘルパーメソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    // DeckManagerの存在確認
    // @returns DeckManagerが存在するかどうか
    // @description DeckManagerのインスタンスが存在するかチェック
    // ----------------------------------------------------------------------
    private bool EnsureDeckManagerExists()
    {
        return DeckManager.Instance != null;
    }
    
    // ----------------------------------------------------------------------
    // デッキアイテム生成パラメータの検証
    // @param deck 検証するデッキモデル
    // @returns パラメータが有効かどうか
    // @description デッキアイテム生成に必要なパラメータを検証
    // ----------------------------------------------------------------------
    private bool ValidateCreateDeckItemParameters(DeckModel deck)
    {
        return deckDetailPrefab != null && contentContainer != null && deck != null;
    }
    
    // ----------------------------------------------------------------------
    // デッキアイテムのインスタンス化
    // @returns 生成されたデッキアイテム
    // @description デッキアイテムのプレハブをインスタンス化し、リストに追加
    // ----------------------------------------------------------------------
    private GameObject InstantiateDeckItem()
    {
        GameObject deckItem = Instantiate(deckDetailPrefab, contentContainer);
        deckItems.Add(deckItem);
        return deckItem;
    }
    
    // ----------------------------------------------------------------------
    // デッキアイテムコンポーネントの設定
    // @param deckItem 設定するデッキアイテム
    // @param deck 設定するデッキモデル
    // @description デッキアイテムコンポーネントを取得し、設定を行う
    // ----------------------------------------------------------------------
    private void SetupDeckItemComponent(GameObject deckItem, DeckModel deck)
    {
        DeckListItem itemComponent = deckItem.GetComponent<DeckListItem>();
        if (itemComponent != null)
        {
            ConfigureDeckItemComponent(itemComponent, deck);
        }
    }
    
    // ----------------------------------------------------------------------
    // デッキアイテムコンポーネントの詳細設定
    // @param itemComponent 設定するアイテムコンポーネント
    // @param deck 設定するデッキモデル
    // @description デッキ情報の設定とイベントリスナーの設定
    // ----------------------------------------------------------------------
    private void ConfigureDeckItemComponent(DeckListItem itemComponent, DeckModel deck)
    {
        itemComponent.SetDeckInfo(deck);
        itemComponent.OnDeckSelected.AddListener(() => OnDeckSelected(deck.Name));
        itemComponent.SetActiveForSampleDeck(deckListPanel, this.gameObject);
    }
    
    // ----------------------------------------------------------------------
    // パネル制御メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    // デッキリストパネルの表示
    // @description デッキリストパネルを表示状態にする
    // ----------------------------------------------------------------------
    private void ShowDeckListPanel()
    {
        if (deckListPanel != null)
        {
            deckListPanel.SetActive(true);
        }
    }
    
    // ----------------------------------------------------------------------
    // パネルを閉じてデッキビューを表示
    // @description サンプルデッキパネルとデッキリストパネルを閉じ、デッキビューを表示
    // ----------------------------------------------------------------------
    private void CloseAndShowDeckView()
    {
        gameObject.SetActive(false);
        
        if (deckListPanel != null)
        {
            deckListPanel.SetActive(false);
        }
        
        if (deckView != null)
        {
            deckView.gameObject.SetActive(true);
        }
    }
    
    // ----------------------------------------------------------------------
    // フィードバックメッセージ表示メソッド
    // ----------------------------------------------------------------------
    
    // ----------------------------------------------------------------------
    // 読み込み中メッセージの表示
    // @param deckName 読み込み中のデッキ名
    // @description 読み込み中のフィードバックメッセージを表示
    // ----------------------------------------------------------------------
    private void ShowLoadingMessage(string deckName)
    {
        if (FeedbackContainer.Instance != null)
        {
            string message = string.Format(Constants.LOADING_MESSAGE_FORMAT, deckName);
            FeedbackContainer.Instance.UpdateFeedbackMessage(message);
        }
    }
    
    // ----------------------------------------------------------------------
    // 表示中メッセージの表示
    // @description デッキ表示中のフィードバックメッセージを表示
    // ----------------------------------------------------------------------
    private void ShowDisplayMessage()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.UpdateFeedbackMessage(Constants.DISPLAY_MESSAGE);
        }
    }
    
    // ----------------------------------------------------------------------
    // 成功メッセージの表示
    // @param deckName 選択されたデッキ名
    // @description デッキ選択成功のフィードバックメッセージを表示
    // ----------------------------------------------------------------------
    private void ShowSuccessMessage(string deckName)
    {
        if (FeedbackContainer.Instance != null)
        {
            string message = string.Format(Constants.SUCCESS_MESSAGE_FORMAT, deckName);
            FeedbackContainer.Instance.ShowSuccessFeedback(message);
        }
    }
    
    // ----------------------------------------------------------------------
    // 選択エラーメッセージの表示
    // @description デッキ選択失敗のエラーメッセージを表示
    // ----------------------------------------------------------------------
    private void ShowSelectionErrorMessage()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowFailureFeedback(Constants.SELECTION_ERROR_MESSAGE);
        }
    }
    
    // ----------------------------------------------------------------------
    // 表示エラーメッセージの表示
    // @description デッキ表示失敗のエラーメッセージを表示
    // ----------------------------------------------------------------------
    private void ShowDisplayErrorMessage()
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowFailureFeedback(Constants.DISPLAY_ERROR_MESSAGE);
        }
    }
}