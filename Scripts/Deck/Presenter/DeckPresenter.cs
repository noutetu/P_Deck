using UnityEngine;
using UniRx;
using System.Threading.Tasks;
using System;

// ----------------------------------------------------------------------
// デッキ画面のPresenterクラス
// DeckModelとDeckViewの仲介役として、ビジネスロジックを処理する
// ----------------------------------------------------------------------
public class DeckPresenter : MonoBehaviour
{
    // ----------------------------------------------------------------------
    // 定数クラス
    // ----------------------------------------------------------------------
    private static class Constants
    {
        // デフォルトメッセージ
        public const string DEFAULT_CARD_NAME = "カード";
        public const string UNKNOWN_ERROR_REASON = "不明なエラー";
        
        // フィードバックメッセージテンプレート
        public const string MSG_CARD_ADDED_SUCCESS = "デッキに追加： 「{0}」";
        public const string MSG_CARD_ADD_FAILED = "デッキに追加できません: {0}";
        public const string MSG_DECK_SIZE_LIMIT = "デッキは最大{0}枚までです";
        public const string MSG_SAME_NAME_LIMIT = "同名カードは{0}枚までです";
        
        // エラーメッセージ
        public const string ERROR_MODEL_NULL = "デッキモデルが設定されていません";
        public const string ERROR_VIEW_NULL = "デッキビューが見つかりません";
        public const string ERROR_INITIALIZATION = "初期化中にエラーが発生しました: {0}";
        public const string ERROR_CARD_ADDITION = "カード追加中にエラーが発生しました: {0}";
        public const string ERROR_DECK_SAVE = "デッキ保存中にエラーが発生しました: {0}";
        public const string ERROR_DECK_CREATION = "デッキ作成中にエラーが発生しました: {0}";
        public const string ERROR_NAME_CHANGE = "デッキ名変更中にエラーが発生しました: {0}";
    }
    // ----------------------------------------------------------------------
    // DeckViewの参照
    // ----------------------------------------------------------------------
    [SerializeField] private DeckView view;

    // ----------------------------------------------------------------------
    // モデル参照
    // ----------------------------------------------------------------------
    private DeckModel model;

    // ----------------------------------------------------------------------
    // Unityライフサイクルメソッド
    // ----------------------------------------------------------------------
    private void Awake()
    {
        try
        {
            ExecuteSafeComponentInitialization();
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_INITIALIZATION, ex.Message));
            Debug.LogException(ex);
        }
    }

    private void OnEnable()
    {
        try
        {
            ExecuteSafeDeckInitialization();
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_INITIALIZATION, ex.Message));
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // DeckViewコンポーネントの安全な初期化処理
    // ----------------------------------------------------------------------
    private void ExecuteSafeComponentInitialization()
    {
        if (view == null)
        {
            view = GetComponent<DeckView>();
            if (view == null)
            {
                Debug.LogError(Constants.ERROR_VIEW_NULL);
                return;
            }
        }
    }

    // ----------------------------------------------------------------------
    // DeckManagerからのデッキモデル取得と初期化処理
    // ----------------------------------------------------------------------
    private void ExecuteSafeDeckInitialization()
    {
        model = DeckManager.Instance.CurrentDeck;
        InitializeModelAndView();
    }

    // ----------------------------------------------------------------------
    // モデルとビューの管理メソッド
    // ----------------------------------------------------------------------
    private async void InitializeModelAndView()
    {
        try
        {
            await ExecuteSafeModelViewInitialization();
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_INITIALIZATION, ex.Message));
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // モデルとビューの安全な初期化実行処理
    // ----------------------------------------------------------------------
    private async Task ExecuteSafeModelViewInitialization()
    {
        if (model == null)
        {
            Debug.LogWarning(Constants.ERROR_MODEL_NULL);
            return;
        }

        await view.DisplayDeck(model);
    }

    public void SetModel(DeckModel newModel)
    {
        try
        {
            ExecuteSafeModelSetup(newModel);
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_INITIALIZATION, ex.Message));
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // 新しいデッキモデルの安全なセットアップ処理
    // ----------------------------------------------------------------------
    private void ExecuteSafeModelSetup(DeckModel newModel)
    {
        model = newModel;
        InitializeModelAndView();
    }

    // ----------------------------------------------------------------------
    // カード操作メソッド
    // ----------------------------------------------------------------------
    public async Task<bool> AddCardToDeck(string cardId)
    {
        try
        {
            return await ExecuteSafeCardAddition(cardId);
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_CARD_ADDITION, ex.Message));
            Debug.LogException(ex);
            ShowFailureFeedback(Constants.UNKNOWN_ERROR_REASON);
            return false;
        }
    }

    // ----------------------------------------------------------------------
    // カード追加処理の安全な実行
    // ----------------------------------------------------------------------
    private async Task<bool> ExecuteSafeCardAddition(string cardId)
    {
        if (model == null)
        {
            Debug.LogWarning(Constants.ERROR_MODEL_NULL);
            return false;
        }

        bool success = model.AddCard(cardId);
        if (success)
        {
            await ProcessSuccessfulCardAddition(cardId);
        }
        else
        {
            ProcessFailedCardAddition(cardId);
        }

        return success;
    }

    // ----------------------------------------------------------------------
    // カード追加成功時の処理（ビュー更新とフィードバック表示）
    // ----------------------------------------------------------------------
    private async Task ProcessSuccessfulCardAddition(string cardId)
    {
        await view.DisplayDeck(model);
        
        CardModel cardModel = model.GetCardModel(cardId);
        string cardName = cardModel != null ? cardModel.name : Constants.DEFAULT_CARD_NAME;
        
        ShowSuccessFeedback(string.Format(Constants.MSG_CARD_ADDED_SUCCESS, cardName));
    }

    // ----------------------------------------------------------------------
    // カード追加失敗時の処理（エラー理由特定とフィードバック表示）
    // ----------------------------------------------------------------------
    private void ProcessFailedCardAddition(string cardId)
    {
        string reason = DetermineCardAdditionFailureReason(cardId);
        ShowFailureFeedback(string.Format(Constants.MSG_CARD_ADD_FAILED, reason));
    }

    // ----------------------------------------------------------------------
    // カード追加失敗の理由を特定する処理
    // ----------------------------------------------------------------------
    private string DetermineCardAdditionFailureReason(string cardId)
    {
        if (model.CardCount >= DeckModel.MAX_CARDS)
        {
            return string.Format(Constants.MSG_DECK_SIZE_LIMIT, DeckModel.MAX_CARDS);
        }

        CardModel cardModel = model.GetCardModel(cardId);
        if (cardModel != null)
        {
            int sameNameCount = model.GetSameNameCardCount(cardModel.name);
            if (sameNameCount >= DeckModel.MAX_SAME_NAME_CARDS)
            {
                return string.Format(Constants.MSG_SAME_NAME_LIMIT, DeckModel.MAX_SAME_NAME_CARDS);
            }
        }

        return Constants.UNKNOWN_ERROR_REASON;
    }

    // ----------------------------------------------------------------------
    // 成功フィードバックメッセージの表示処理
    // ----------------------------------------------------------------------
    private void ShowSuccessFeedback(string message)
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowSuccessFeedback(message);
        }
    }

    // ----------------------------------------------------------------------
    // 失敗フィードバックメッセージの表示処理
    // ----------------------------------------------------------------------
    private void ShowFailureFeedback(string message)
    {
        if (FeedbackContainer.Instance != null)
        {
            FeedbackContainer.Instance.ShowFailureFeedback(message);
        }
    }
    // ----------------------------------------------------------------------
    // デッキ管理メソッド
    // ----------------------------------------------------------------------
    public void SaveDeck()
    {
        try
        {
            ExecuteSafeDeckSave();
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_DECK_SAVE, ex.Message));
            Debug.LogException(ex);
        }
    }

    public async void CreateNewDeck()
    {
        try
        {
            await ExecuteSafeNewDeckCreation();
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_DECK_CREATION, ex.Message));
            Debug.LogException(ex);
        }
    }

    public void ChangeDeckName(string newName)
    {
        try
        {
            ExecuteSafeDeckNameChange(newName);
        }
        catch (Exception ex)
        {
            Debug.LogError(string.Format(Constants.ERROR_NAME_CHANGE, ex.Message));
            Debug.LogException(ex);
        }
    }

    // ----------------------------------------------------------------------
    // デッキ保存処理の安全な実行
    // ----------------------------------------------------------------------
    private void ExecuteSafeDeckSave()
    {
        DeckManager.Instance.SaveCurrentDeck();
    }

    // ----------------------------------------------------------------------
    // 新しいデッキ作成処理の安全な実行
    // ----------------------------------------------------------------------
    private async Task ExecuteSafeNewDeckCreation()
    {
        model = DeckManager.Instance.CreateNewDeck();
        await view.DisplayDeck(model);
    }

    // ----------------------------------------------------------------------
    // デッキ名変更処理の安全な実行
    // ----------------------------------------------------------------------
    private void ExecuteSafeDeckNameChange(string newName)
    {
        if (model != null && !string.IsNullOrEmpty(newName))
        {
            model.Name = newName;
        }
    }
}