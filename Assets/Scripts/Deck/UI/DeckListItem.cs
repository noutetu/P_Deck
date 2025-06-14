using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

// ----------------------------------------------------------------------
// デッキ一覧パネルの各デッキアイテムを管理するクラス
// ----------------------------------------------------------------------
public class DeckListItem : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // 定数クラス
    // ----------------------------------------------------------------------
    private static class Constants
    {
        public const int DEFAULT_HP_VALUE = 0;
        public const string DECK_NAME_CHANGE_MESSAGE_FORMAT = "デッキ名を変更しました: {0} → {1}";
        public const string DECK_DELETE_SUCCESS_MESSAGE_FORMAT = "デッキを削除しました: {0}";
        public const string DECK_DELETE_FAILURE_MESSAGE_FORMAT = "デッキの削除に失敗しました: {0}";
        public const string SAMPLE_DECK_COPY_MESSAGE_FORMAT = "サンプルデッキ '{0}' を通常デッキにコピーしました: '{1}'";
        public const string DECK_COPY_ERROR_MESSAGE_FORMAT = "デッキのコピー中にエラーが発生しました: {0}";
    }

    // ----------------------------------------------------------------------
    // フィールド
    // ----------------------------------------------------------------------
    [SerializeField] private TMP_InputField deckNameInput;      // デッキ名入力フィールド
    [SerializeField] private RawImage cardImage;        // デッキアイコン画像
    [SerializeField] private Button selectButton;       // デッキ選択ボタン
    [SerializeField] private Button deleteButton;       // デッキ削除ボタン
    [SerializeField] private Button copyButton;        // デッキコピー用ボタン

    // パネル
    private GameObject deckListPanel; // 親のデッキ一覧パネル
    private GameObject sampleDeckPanel; // サンプルデッキパネル

    // デッキ選択イベント
    public UnityEvent OnDeckSelected = new UnityEvent();

    private DeckModel currentDeck;  // 現在のデッキ情報 

    // ======================================================================
    // ライフサイクルメソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // Unityの初期化処理 - イベントリスナーの設定
    // ----------------------------------------------------------------------
    private void Start()
    {
        SetupEventListeners();
    }

    // ----------------------------------------------------------------------
    // イベントリスナーの設定
    // ----------------------------------------------------------------------
    private void SetupEventListeners()
    {
        SetupSelectButton();
        SetupDeckNameInput();
        SetupDeleteButton();
        SetupCopyButton();
    }

    // ----------------------------------------------------------------------
    // 選択ボタンのイベント設定
    // ----------------------------------------------------------------------
    private void SetupSelectButton()
    {
        if (selectButton != null)
        {
            selectButton.onClick.AddListener(() =>
            {
                OnDeckSelected.Invoke();
            });
        }
    }

    // ----------------------------------------------------------------------
    // デッキ名入力フィールドのイベント設定
    // ----------------------------------------------------------------------
    private void SetupDeckNameInput()
    {
        if (deckNameInput != null)
        {
            deckNameInput.onEndEdit.AddListener(OnDeckNameChanged);
        }
    }

    // ----------------------------------------------------------------------
    // 削除ボタンのイベント設定
    // ----------------------------------------------------------------------
    private void SetupDeleteButton()
    {
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }
    }

    // ----------------------------------------------------------------------
    // コピーボタンのイベント設定
    // ----------------------------------------------------------------------
    private void SetupCopyButton()
    {
        if (copyButton != null)
        {
            copyButton.onClick.AddListener(OnCopyButtonClicked);
        }
    }

    // ======================================================================
    // 公開メソッド
    // ======================================================================

    // ----------------------------------------------------------------------
    // デッキ情報を設定
    // @param deck 設定するデッキモデル
    // ----------------------------------------------------------------------
    public void SetDeckInfo(DeckModel deck)
    {
        currentDeck = deck;

        if (deckNameInput != null && deck != null)
        {
            deckNameInput.text = deck.Name;
        }

        // 最も体力の高いポケモンをアイコンに設定
        SetHighestHPPokemonAsIcon(deck);
    }

    // ----------------------------------------------------------------------
    // デッキ名変更時の処理
    // @param newName 新しいデッキ名
    // ----------------------------------------------------------------------
    private void OnDeckNameChanged(string newName)
    {
        if (currentDeck == null)
            return;
        // 空の場合は何もしない（元の名前を維持）
        if (string.IsNullOrEmpty(newName))
        {
            deckNameInput.text = currentDeck.Name;
            return;
        }

        // 現在のデッキ名と異なる場合のみ保存処理
        if (currentDeck.Name != newName)
        {
            // デッキ名を更新
            string oldName = currentDeck.Name;
            currentDeck.Name = newName;

            // DeckManagerに変更を保存
            if (DeckManager.Instance != null)
            {
                DeckManager.Instance.SaveCurrentDeck();

                // フィードバック表示
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowSuccessFeedback(string.Format(Constants.DECK_NAME_CHANGE_MESSAGE_FORMAT, oldName, newName));
                }
            }
        }
    }

    // ----------------------------------------------------------------------
    // 最も体力の高いポケモンをアイコンに設定
    // @param deck 対象のデッキモデル
    // ----------------------------------------------------------------------
    private async void SetHighestHPPokemonAsIcon(DeckModel deck)
    {
        if (cardImage == null || deck == null)
            return;

        CardModel highestHPCard = FindHighestHPPokemon(deck);
        await SetCardImageTexture(highestHPCard);
    }

    // ----------------------------------------------------------------------
    // デッキから最も体力の高いポケモンを検索
    // @param deck 対象のデッキモデル
    // @returns 最も体力の高いポケモンカード（見つからない場合はnull）
    // ----------------------------------------------------------------------
    private CardModel FindHighestHPPokemon(DeckModel deck)
    {
        CardModel highestHPCard = null;
        int highestHP = Constants.DEFAULT_HP_VALUE;

        // デッキ内のすべてのカードをチェック
        foreach (string cardId in deck.CardIds)
        {
            CardModel card = deck.GetCardModel(cardId);

            // ポケモンカードで、HPが最高値のカードを探す
            if (IsPokemonCardWithHigherHP(card, highestHP))
            {
                highestHP = card.hp;
                highestHPCard = card;
            }
        }

        return highestHPCard;
    }

    // ----------------------------------------------------------------------
    // ポケモンカードでより高いHPを持つかどうかを判定
    // @param card チェック対象のカード
    // @param currentHighestHP 現在の最高HP値
    // @returns より高いHPを持つポケモンカードかどうか
    // ----------------------------------------------------------------------
    private bool IsPokemonCardWithHigherHP(CardModel card, int currentHighestHP)
    {
        return card != null &&
               (card.cardTypeEnum == Enum.CardType.非EX || card.cardTypeEnum == Enum.CardType.EX) &&
               card.hp > currentHighestHP;
    }

    // ----------------------------------------------------------------------
    // カード画像のテクスチャを設定
    // @param card 設定するカード（nullの場合はデフォルトテクスチャ）
    // ----------------------------------------------------------------------
    private async System.Threading.Tasks.Task SetCardImageTexture(CardModel card)
    {
        if (card != null)
        {
            await SetPokemonCardTexture(card);
        }
        else
        {
            SetDefaultTexture();
        }
    }

    // ----------------------------------------------------------------------
    // ポケモンカードのテクスチャを設定
    // @param card 設定するポケモンカード
    // ----------------------------------------------------------------------
    private async System.Threading.Tasks.Task SetPokemonCardTexture(CardModel card)
    {
        // ImageCacheManagerを使用してキャッシュを確認
        if (ImageCacheManager.Instance != null)
        {
            await SetTextureFromCache(card);
        }
        else if (card.imageTexture != null)
        {
            // ImageCacheManagerが利用できない場合は既存のテクスチャを使用
            cardImage.texture = card.imageTexture;
        }
    }

    // ----------------------------------------------------------------------
    // キャッシュからテクスチャを設定
    // @param card 設定するポケモンカード
    // ----------------------------------------------------------------------
    private async System.Threading.Tasks.Task SetTextureFromCache(CardModel card)
    {
        // キャッシュされている場合は即座に設定
        if (ImageCacheManager.Instance.IsCardTextureCached(card))
        {
            SetCachedTexture(card);
        }
        else
        {
            // キャッシュにない場合は非同期で読み込み
            await LoadTextureAsync(card);
        }
    }

    // ----------------------------------------------------------------------
    // キャッシュされたテクスチャを設定
    // @param card 設定するポケモンカード
    // ----------------------------------------------------------------------
    private void SetCachedTexture(CardModel card)
    {
        Texture2D cachedTexture = ImageCacheManager.Instance.GetCachedCardTexture(card);
        cardImage.texture = cachedTexture;
        card.imageTexture = cachedTexture;
    }

    // ----------------------------------------------------------------------
    // 非同期でテクスチャを読み込み
    // @param card 設定するポケモンカード
    // ----------------------------------------------------------------------
    private async System.Threading.Tasks.Task LoadTextureAsync(CardModel card)
    {
        try
        {
            Texture2D texture = await ImageCacheManager.Instance.GetCardTextureAsync(card);
            if (cardImage != null && texture != null) // UI要素がまだ有効かチェック
            {
                cardImage.texture = texture;
            }
        }
        catch (System.Exception)
        {
            // エラー時はデフォルトテクスチャを設定
            SetDefaultTexture();
        }
    }

    // ----------------------------------------------------------------------
    // デフォルトテクスチャを設定
    // ----------------------------------------------------------------------
    private void SetDefaultTexture()
    {
        if (cardImage != null && ImageCacheManager.Instance != null)
        {
            cardImage.texture = ImageCacheManager.Instance.GetDefaultTexture();
        }
    }

    // ----------------------------------------------------------------------
    // 削除ボタンクリック時の処理
    // ----------------------------------------------------------------------
    private void OnDeleteButtonClicked()
    {
        if (currentDeck == null)
            return;

        // デッキ名を保持（フィードバック表示用）
        string deckName = currentDeck.Name;

        // DeckManagerを使用してデッキを削除
        if (DeckManager.Instance != null)
        {
            bool success = DeckManager.Instance.DeleteDeck(deckName);

            if (success)
            {
                // フィードバック表示
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowSuccessFeedback(string.Format(Constants.DECK_DELETE_SUCCESS_MESSAGE_FORMAT, deckName));
                }

                // 親のDeckListPanelを取得して更新を通知
                var deckListPanel = GetComponentInParent<DeckListPanel>();
                if (deckListPanel != null)
                {
                    deckListPanel.RefreshDeckList();
                }
            }
            else
            {
                // 削除失敗時のフィードバック
                if (FeedbackContainer.Instance != null)
                {
                    FeedbackContainer.Instance.ShowFailureFeedback(string.Format(Constants.DECK_DELETE_FAILURE_MESSAGE_FORMAT, deckName));
                }
            }
        }
    }
    // ----------------------------------------------------------------------
    // コピー用ボタンクリック時の処理
    // ----------------------------------------------------------------------
    private async void OnCopyButtonClicked()
    {
        if (currentDeck == null)
            return;

        SetCopyButtonState(false);

        try
        {
            await ExecuteDeckCopyProcess();
        }
        catch (System.Exception ex)
        {
            HandleCopyError(ex);
        }
        finally
        {
            SetCopyButtonState(true);
        }
    }

    // ----------------------------------------------------------------------
    // コピーボタンの有効/無効状態を設定
    // @param isEnabled ボタンを有効にするかどうか
    // ----------------------------------------------------------------------
    private void SetCopyButtonState(bool isEnabled)
    {
        if (copyButton != null)
            copyButton.interactable = isEnabled;
    }

    // ----------------------------------------------------------------------
    // デッキコピー処理を実行
    // ----------------------------------------------------------------------
    private async System.Threading.Tasks.Task ExecuteDeckCopyProcess()
    {
        string originalDeckName = currentDeck.Name;

        if (DeckManager.Instance != null)
        {
            bool isSampleDeck = DeckManager.Instance.IsSampleDeck(originalDeckName);
            DeckModel copiedDeck = await DeckManager.Instance.CopyDeckAsync(originalDeckName);
            
            if (copiedDeck != null)
            {
                ShowCopySuccessFeedback(isSampleDeck, originalDeckName, copiedDeck.Name);
            }
            
            UpdatePanelVisibility();
        }
    }

    // ----------------------------------------------------------------------
    // コピー成功時のフィードバック表示
    // @param isSampleDeck サンプルデッキかどうか
    // @param originalName 元のデッキ名
    // @param copiedName コピー後のデッキ名
    // ----------------------------------------------------------------------
    private void ShowCopySuccessFeedback(bool isSampleDeck, string originalName, string copiedName)
    {
        if (isSampleDeck && FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowSuccessFeedback(string.Format(Constants.SAMPLE_DECK_COPY_MESSAGE_FORMAT, originalName, copiedName));
        }
    }

    // ----------------------------------------------------------------------
    // パネルの表示状態を更新
    // ----------------------------------------------------------------------
    private void UpdatePanelVisibility()
    {
        if (deckListPanel != null)
        {
            deckListPanel.gameObject.SetActive(true);
        }
        if (sampleDeckPanel != null)
        {
            sampleDeckPanel.gameObject.SetActive(false);
        }
    }

    // ----------------------------------------------------------------------
    // コピーエラー時のハンドリング
    // @param ex 発生した例外
    // ----------------------------------------------------------------------
    private void HandleCopyError(System.Exception ex)
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowFailureFeedback(string.Format(Constants.DECK_COPY_ERROR_MESSAGE_FORMAT, ex.Message));
        }
    }

    // ----------------------------------------------------------------------
    // サンプルデッキ用のアクティブ設定
    // @param deckListPanel デッキ一覧パネル
    // @param sampleDeckPanel サンプルデッキパネル
    // ----------------------------------------------------------------------
    public void SetActiveForSampleDeck(GameObject deckListPanel, GameObject sampleDeckPanel)
    {
        this.deckListPanel = deckListPanel;
        this.sampleDeckPanel = sampleDeckPanel;

        deleteButton.gameObject.SetActive(false);
        deckNameInput.readOnly = true;
    }
}