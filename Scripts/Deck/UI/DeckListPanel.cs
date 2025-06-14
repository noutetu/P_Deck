using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

// ----------------------------------------------------------------------
// デッキ一覧パネルを管理するクラス
// ----------------------------------------------------------------------
public class DeckListPanel : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // 定数クラス
    // ----------------------------------------------------------------------
    private static class Constants
    {
        public const int MIN_DECK_COUNT = 0;
    }

    // ----------------------------------------------------------------------
    // フィールド変数
    // ----------------------------------------------------------------------
    [SerializeField] private Transform contentContainer;    // デッキ一覧のコンテナ
    [SerializeField] private GameObject deckDetailPrefab;   // デッキ詳細アイテムのプレハブ
    [SerializeField] private GameObject deckPanel;       // デッキパネル
    [SerializeField] private DeckView deckView;       // デッキビュー
    [SerializeField] private Button closeButton;       // 閉じるボタン
    [SerializeField] private Button toSampleDeckListButton; // サンプルデッキ一覧へ移動ボタン
    [SerializeField] private SampleDeckPanel sampleDeckPanel; // サンプルデッキパネル

    [Header("NoDeckMessage")]
    [SerializeField] private GameObject noDeckMessage; // デッキがない場合のメッセージ

    private List<GameObject> deckItems = new List<GameObject>();    // デッキアイテムのリスト

    // ======================================================================
    // ライフサイクルメソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // Unityの初期化メソッド
    // ----------------------------------------------------------------------
    private void OnEnable()
    {
        // パネルが表示されるたびにデッキリストを更新
        RefreshDeckList();

        // DeckViewを非表示にする
        if (deckPanel != null)
        {
            deckPanel.SetActive(false);
        }
    }

    // ----------------------------------------------------------------------
    // Unityの初期化メソッド(初回のみ) - イベントリスナーの設定
    // ----------------------------------------------------------------------
    private void Start()
    {
        SetupEventListeners();
    }

    // ======================================================================
    // 初期化・セットアップメソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // イベントリスナーの設定
    // ----------------------------------------------------------------------
    private void SetupEventListeners()
    {
        SetupCloseButton();
        SetupSampleDeckListButton();
    }

    // ----------------------------------------------------------------------
    // 閉じるボタンのイベント設定
    // ----------------------------------------------------------------------
    private void SetupCloseButton()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonClicked);
        }
    }

    // ----------------------------------------------------------------------
    // サンプルデッキ一覧へ移動ボタンのイベント設定
    // ----------------------------------------------------------------------
    private void SetupSampleDeckListButton()
    {
        if (toSampleDeckListButton != null)
        {
            toSampleDeckListButton.onClick.AddListener(GoToSampleDeckList);
        }
    }

    // ======================================================================
    // 公開メソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // デッキ一覧を最新の状態に更新
    // ----------------------------------------------------------------------
    public void RefreshDeckList()
    {
        ClearDeckItems();
        
        if (HasNoDecks())
        {
            ShowNoDeckMessage();
            return;
        }
        
        HideNoDeckMessage();
        CreateAllDeckItems();
    }

    // ----------------------------------------------------------------------
    // サンプルデッキ一覧へ移動
    // @param なし
    // ----------------------------------------------------------------------   
    public void GoToSampleDeckList()
    {
        ShowSampleDeckPanel();
        HideCurrentPanel();
    }

    // ======================================================================
    // イベントハンドラー
    // ======================================================================

    // ----------------------------------------------------------------------
    // 閉じるボタンクリック時の処理
    // ----------------------------------------------------------------------
    private void OnCloseButtonClicked()
    {
        ClosePanelAndShowDeck();
    }

    // ----------------------------------------------------------------------
    // デッキ選択時の処理
    // @param deckName 選択されたデッキ名
    // ----------------------------------------------------------------------
    private async void SelectDeck(string deckName)
    {
        if (DeckManager.Instance != null)
        {
            DeckManager.Instance.SelectDeck(deckName);
            await DisplaySelectedDeck();
            ShowDeckPanelAndHideCurrent();
        }
    }

    // ----------------------------------------------------------------------
    // デッキが存在しないかどうかを判定
    // @returns デッキが存在しないかどうか
    // ----------------------------------------------------------------------
    private bool HasNoDecks()
    {
        return DeckManager.Instance == null || 
               DeckManager.Instance.SavedDecks.Count == Constants.MIN_DECK_COUNT;
    }

    // ----------------------------------------------------------------------
    // すべてのデッキアイテムを作成
    // ----------------------------------------------------------------------
    private void CreateAllDeckItems()
    {
        if (DeckManager.Instance != null)
        {
            foreach (var deck in DeckManager.Instance.SavedDecks)
            {
                CreateDeckItem(deck);
            }
        }
    }

    // ----------------------------------------------------------------------
    // デッキアイテムをすべて削除
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
    // "デッキなし"メッセージを表示
    // ----------------------------------------------------------------------
    private void ShowNoDeckMessage()
    {
        if (noDeckMessage != null)
        {
            noDeckMessage.SetActive(true);
        }
    }

    // ----------------------------------------------------------------------
    // "デッキなし"メッセージを非表示
    // ----------------------------------------------------------------------
    private void HideNoDeckMessage()
    {
        if (noDeckMessage != null)
        {
            noDeckMessage.SetActive(false);
        }
    }

    // ----------------------------------------------------------------------
    // サンプルデッキパネルを表示
    // ----------------------------------------------------------------------
    private void ShowSampleDeckPanel()
    {
        if (sampleDeckPanel != null)
        {
            sampleDeckPanel.gameObject.SetActive(true);
        }
    }

    // ----------------------------------------------------------------------
    // 現在のパネルを非表示
    // ----------------------------------------------------------------------
    private void HideCurrentPanel()
    {
        gameObject.SetActive(false);
    }

    // ----------------------------------------------------------------------
    // パネルを閉じてデッキを表示
    // ----------------------------------------------------------------------
    private void ClosePanelAndShowDeck()
    {
        HideCurrentPanel();
        ShowDeckPanel();
        ClearDeckViewIfNeeded();
    }

    // ----------------------------------------------------------------------
    // デッキパネルを表示
    // ----------------------------------------------------------------------
    private void ShowDeckPanel()
    {
        if (deckPanel != null)
        {
            deckPanel.SetActive(true);
        }
    }

    // ----------------------------------------------------------------------
    // 必要に応じてデッキビューをクリア
    // ----------------------------------------------------------------------
    private void ClearDeckViewIfNeeded()
    {
        if (ShouldClearDeckView())
        {
            ClearDeckView();
        }
    }

    // ----------------------------------------------------------------------
    // デッキビューをクリアすべきかどうかを判定
    // @returns デッキビューをクリアすべきかどうか
    // ----------------------------------------------------------------------
    private bool ShouldClearDeckView()
    {
        return DeckManager.Instance != null && 
               DeckManager.Instance.CurrentDeck == null && 
               deckView != null;
    }

    // ----------------------------------------------------------------------
    // デッキビューをクリア
    // ----------------------------------------------------------------------
    private void ClearDeckView()
    {
        if (deckView != null)
        {
            _ = deckView.DisplayDeck(null);
        }
    }

    // ----------------------------------------------------------------------
    // 選択されたデッキを表示
    // @returns 非同期タスク
    // ----------------------------------------------------------------------
    private async Task DisplaySelectedDeck()
    {
        if (deckView != null)
        {
            await deckView.DisplayDeck(DeckManager.Instance.CurrentDeck);
        }
    }

    // ----------------------------------------------------------------------
    // デッキパネルを表示し、現在のパネルを非表示
    // ----------------------------------------------------------------------
    private void ShowDeckPanelAndHideCurrent()
    {
        HideCurrentPanel();
        ShowDeckPanel();
    }

    // ----------------------------------------------------------------------
    // デッキアイテムを生成
    // @param deck 生成するデッキのモデル
    // ----------------------------------------------------------------------
    private void CreateDeckItem(DeckModel deck)
    {
        if (!CanCreateDeckItem())
            return;

        GameObject deckItem = CreateDeckItemGameObject();
        SetupDeckItemComponent(deckItem, deck);
    }

    // ----------------------------------------------------------------------
    // デッキアイテムを作成可能かどうかを判定
    // @returns デッキアイテムを作成可能かどうか
    // ----------------------------------------------------------------------
    private bool CanCreateDeckItem()
    {
        return deckDetailPrefab != null && contentContainer != null;
    }

    // ----------------------------------------------------------------------
    // デッキアイテムのGameObjectを作成
    // @returns 作成されたGameObject
    // ----------------------------------------------------------------------
    private GameObject CreateDeckItemGameObject()
    {
        GameObject deckItem = Instantiate(deckDetailPrefab, contentContainer);
        deckItems.Add(deckItem);
        return deckItem;
    }

    // ----------------------------------------------------------------------
    // デッキアイテムコンポーネントを設定
    // @param deckItem 設定対象のGameObject
    // @param deck デッキモデル
    // ----------------------------------------------------------------------
    private void SetupDeckItemComponent(GameObject deckItem, DeckModel deck)
    {
        DeckListItem itemComponent = deckItem.GetComponent<DeckListItem>();
        if (itemComponent != null)
        {
            itemComponent.SetDeckInfo(deck);
            SetupDeckItemClickEvent(itemComponent, deck);
        }
    }

    // ----------------------------------------------------------------------
    // デッキアイテムのクリックイベントを設定
    // @param itemComponent デッキリストアイテムコンポーネント
    // @param deck デッキモデル
    // ----------------------------------------------------------------------
    private void SetupDeckItemClickEvent(DeckListItem itemComponent, DeckModel deck)
    {
        itemComponent.OnDeckSelected.AddListener(() =>
        {
            SelectDeck(deck.Name);
        });
    }
}