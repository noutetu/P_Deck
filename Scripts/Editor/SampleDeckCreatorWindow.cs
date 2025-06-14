using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace PokeDeck.Editor
{
    // ----------------------------------------------------------------------
    // サンプルデッキ作成用のエディターウィンドウ
    // DeckManagerのSampleDeckConfigを効率的に作成・編集するためのツール
    // ----------------------------------------------------------------------
    public class SampleDeckCreatorWindow : EditorWindow
    {
        // ----------------------------------------------------------------------
        // 定数管理
        // ----------------------------------------------------------------------
        private static class Constants
        {
            // ウィンドウ設定
            public const string WINDOW_TITLE = "Sample Deck Creator";
            public const int WINDOW_MIN_WIDTH = 400;
            public const int WINDOW_MIN_HEIGHT = 600;
            
            // デフォルト値
            public const int DEFAULT_MAX_CARDS = 20;
            public const int MIN_CARDS = 1;
            public const int MAX_CARDS = 60;
            public const int TEXT_AREA_HEIGHT = 60;
            public const int DELETE_BUTTON_WIDTH = 20;
            public const int CREATE_BUTTON_HEIGHT = 30;
            
            // エネルギー選択制限
            public const int MAX_ENERGY_TYPE_SELECTION = 2;
            
            // UI ラベルテキスト
            public const string BASIC_INFO_LABEL = "基本情報";
            public const string DECK_NAME_LABEL = "デッキ名";
            public const string DECK_MEMO_LABEL = "デッキメモ";
            public const string MAX_CARDS_LABEL = "最大カード数";
            public const string ENERGY_TYPE_LABEL = "エネルギータイプ (最大2つ)";
            public const string CARD_ID_LABEL = "カードID";
            public const string CARD_LABEL_FORMAT = "カード {0}";
            public const string ACTION_LABEL = "アクション";
            public const string PREVIEW_LABEL = "プレビュー";
            
            // ボタンテキスト
            public const string DELETE_BUTTON_TEXT = "×";
            public const string ADD_CARD_BUTTON_TEXT = "カードIDを追加";
            public const string REMOVE_EMPTY_BUTTON_TEXT = "空のエントリを削除";
            public const string CREATE_DECK_BUTTON_TEXT = "サンプルデッキ設定を作成";
            public const string CLEAR_FORM_BUTTON_TEXT = "フォームをクリア";
            
            // プレビューテキスト
            public const string DECK_NAME_PREVIEW_FORMAT = "デッキ名: {0}";
            public const string MAX_CARDS_PREVIEW_FORMAT = "最大カード数: {0}";
            public const string ENERGY_TYPE_PREVIEW_FORMAT = "エネルギータイプ: {0}";
            public const string CARD_ID_COUNT_PREVIEW_FORMAT = "カードID数: {0}";
            public const string SELECTED_ENERGY_FORMAT = "選択中: {0}";
            public const string NOT_SET_TEXT = "未設定";
            public const string NOT_SELECTED_TEXT = "未選択";
            
            // メッセージテキスト
            public const string ENERGY_LIMIT_TITLE = "選択制限";
            public const string ENERGY_LIMIT_MESSAGE = "エネルギータイプは最大2つまで選択できます。";
            public const string CLEAR_CONFIRM_TITLE = "確認";
            public const string CLEAR_CONFIRM_MESSAGE = "フォームの内容をクリアしますか？";
            public const string SUCCESS_TITLE = "成功";
            public const string SUCCESS_MESSAGE_FORMAT = "サンプルデッキ '{0}' を作成しました！";
            public const string ERROR_TITLE = "エラー";
            public const string DECK_MANAGER_NOT_FOUND_MESSAGE = "シーン内にDeckManagerが見つかりません。";
            public const string CREATION_ERROR_MESSAGE_FORMAT = "サンプルデッキの作成中にエラーが発生しました: {0}";
            public const string OK_BUTTON_TEXT = "OK";
            public const string YES_BUTTON_TEXT = "はい";
            public const string NO_BUTTON_TEXT = "いいえ";
            
            // SerializedProperty名
            public const string SAMPLE_DECKS_PROPERTY = "sampleDecks";
            public const string DECK_NAME_PROPERTY = "deckName";
            public const string DECK_MEMO_PROPERTY = "deckMemo";
            public const string MAX_CARDS_PROPERTY = "maxCards";
            public const string ENERGY_TYPES_PROPERTY = "energyTypes";
            public const string SPECIFIC_CARD_IDS_PROPERTY = "specificCardIds";
            
            // エネルギータイプ配列（定数として定義）
            public static readonly (string displayName, Enum.PokemonType type)[] AVAILABLE_ENERGY_TYPES = new[]
            {
                ("草", Enum.PokemonType.草),
                ("炎", Enum.PokemonType.炎),
                ("水", Enum.PokemonType.水),
                ("雷", Enum.PokemonType.雷),
                ("超", Enum.PokemonType.超),
                ("闘", Enum.PokemonType.闘),
                ("悪", Enum.PokemonType.悪),
                ("鋼", Enum.PokemonType.鋼)
            };
        }

        // ----------------------------------------------------------------------
        // フィールド変数
        // ----------------------------------------------------------------------
        private string deckName = "";
        private string deckMemo = "";
        private int maxCards = Constants.DEFAULT_MAX_CARDS;
        private Vector2 scrollPosition;
        private List<bool> energyTypeSelected = new List<bool>();
        private List<string> cardIds = new List<string>();

        // ======================================================================
        // Unity エディターライフサイクルメソッド
        // ======================================================================

        // ----------------------------------------------------------------------
        // メニューアイテム
        // ----------------------------------------------------------------------
        [MenuItem("PokeDeck/Sample Deck Creator")]
        public static void ShowWindow()
        {
            var window = GetWindow<SampleDeckCreatorWindow>(Constants.WINDOW_TITLE);
            window.minSize = new Vector2(Constants.WINDOW_MIN_WIDTH, Constants.WINDOW_MIN_HEIGHT);
            window.Show();
        }

        // ----------------------------------------------------------------------
        // 初期化
        // ----------------------------------------------------------------------
        private void OnEnable()
        {
            // エネルギータイプ選択状態を初期化
            energyTypeSelected = new List<bool>(new bool[Constants.AVAILABLE_ENERGY_TYPES.Length]);
            
            // カードID入力フィールドを初期化
            if (cardIds.Count == 0)
            {
                cardIds.Add("");
            }
        }

        // ----------------------------------------------------------------------
        // GUI描画
        // ----------------------------------------------------------------------
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.LabelField(Constants.WINDOW_TITLE, EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawBasicInfo();
            EditorGUILayout.Space();

            DrawEnergyTypeSelector();
            EditorGUILayout.Space();

            DrawCardIdList();
            EditorGUILayout.Space();

            DrawActionButtons();

            EditorGUILayout.EndScrollView();
        }

        // ======================================================================
        // GUI 描画メソッド
        // ======================================================================

        // ----------------------------------------------------------------------
        // 基本情報セクション
        // ----------------------------------------------------------------------
        private void DrawBasicInfo()
        {
            EditorGUILayout.LabelField(Constants.BASIC_INFO_LABEL, EditorStyles.boldLabel);
            
            deckName = EditorGUILayout.TextField(Constants.DECK_NAME_LABEL, deckName);
            
            EditorGUILayout.LabelField(Constants.DECK_MEMO_LABEL);
            deckMemo = EditorGUILayout.TextArea(deckMemo, GUILayout.Height(Constants.TEXT_AREA_HEIGHT));
            
            maxCards = EditorGUILayout.IntSlider(Constants.MAX_CARDS_LABEL, maxCards, Constants.MIN_CARDS, Constants.MAX_CARDS);
        }

        // ----------------------------------------------------------------------
        // エネルギータイプ選択セクション
        // ----------------------------------------------------------------------
        private void DrawEnergyTypeSelector()
        {
            EditorGUILayout.LabelField(Constants.ENERGY_TYPE_LABEL, EditorStyles.boldLabel);
            
            int selectedCount = energyTypeSelected.Count(x => x);
            
            for (int i = 0; i < Constants.AVAILABLE_ENERGY_TYPES.Length; i++)
            {
                bool wasSelected = energyTypeSelected[i];
                bool isSelected = EditorGUILayout.Toggle(Constants.AVAILABLE_ENERGY_TYPES[i].displayName, wasSelected);
                
                // 選択状態が変更された場合の処理
                if (isSelected != wasSelected)
                {
                    if (isSelected && selectedCount >= Constants.MAX_ENERGY_TYPE_SELECTION)
                    {
                        // 既に2つ選択されている場合は選択を無効にする
                        EditorUtility.DisplayDialog(Constants.ENERGY_LIMIT_TITLE, Constants.ENERGY_LIMIT_MESSAGE, Constants.OK_BUTTON_TEXT);
                        continue;
                    }
                    
                    energyTypeSelected[i] = isSelected;
                }
            }
            
            // 選択中のエネルギータイプを表示
            var selectedTypes = GetSelectedEnergyTypes();
            if (selectedTypes.Count > 0)
            {
                EditorGUILayout.LabelField(string.Format(Constants.SELECTED_ENERGY_FORMAT, string.Join(", ", selectedTypes.Select(t => t.ToString()))));
            }
        }

        // ----------------------------------------------------------------------
        // カードIDリストセクション
        // ----------------------------------------------------------------------
        private void DrawCardIdList()
        {
            EditorGUILayout.LabelField(Constants.CARD_ID_LABEL, EditorStyles.boldLabel);
            
            for (int i = 0; i < cardIds.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                cardIds[i] = EditorGUILayout.TextField(string.Format(Constants.CARD_LABEL_FORMAT, i + 1), cardIds[i]);
                
                if (GUILayout.Button(Constants.DELETE_BUTTON_TEXT, GUILayout.Width(Constants.DELETE_BUTTON_WIDTH)) && cardIds.Count > 1)
                {
                    cardIds.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(Constants.ADD_CARD_BUTTON_TEXT))
            {
                cardIds.Add("");
            }
            
            if (GUILayout.Button(Constants.REMOVE_EMPTY_BUTTON_TEXT))
            {
                cardIds.RemoveAll(string.IsNullOrEmpty);
                if (cardIds.Count == 0)
                {
                    cardIds.Add("");
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        // ----------------------------------------------------------------------
        // アクションボタンセクション
        // ----------------------------------------------------------------------
        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField(Constants.ACTION_LABEL, EditorStyles.boldLabel);
            
            // プレビュー情報
            DrawPreviewInfo();
            
            EditorGUILayout.Space();
            
            // 作成ボタン
            GUI.enabled = !string.IsNullOrEmpty(deckName);
            if (GUILayout.Button(Constants.CREATE_DECK_BUTTON_TEXT, GUILayout.Height(Constants.CREATE_BUTTON_HEIGHT)))
            {
                CreateSampleDeckConfig();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space();
            
            // クリアボタン
            if (GUILayout.Button(Constants.CLEAR_FORM_BUTTON_TEXT))
            {
                ClearForm();
            }
        }

        // ======================================================================
        // データ取得・処理メソッド
        // ======================================================================

        // ----------------------------------------------------------------------
        // プレビュー情報表示
        // ----------------------------------------------------------------------
        private void DrawPreviewInfo()
        {
            EditorGUILayout.LabelField(Constants.PREVIEW_LABEL, EditorStyles.boldLabel);
            
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(string.Format(Constants.DECK_NAME_PREVIEW_FORMAT, (string.IsNullOrEmpty(deckName) ? Constants.NOT_SET_TEXT : deckName)));
            EditorGUILayout.LabelField(string.Format(Constants.MAX_CARDS_PREVIEW_FORMAT, maxCards));
            
            var selectedTypes = GetSelectedEnergyTypes();
            EditorGUILayout.LabelField(string.Format(Constants.ENERGY_TYPE_PREVIEW_FORMAT, (selectedTypes.Count > 0 ? string.Join(", ", selectedTypes) : Constants.NOT_SELECTED_TEXT)));
            
            var validCardIds = cardIds.Where(id => !string.IsNullOrEmpty(id)).ToList();
            EditorGUILayout.LabelField(string.Format(Constants.CARD_ID_COUNT_PREVIEW_FORMAT, validCardIds.Count));
            EditorGUI.indentLevel--;
        }

        // ----------------------------------------------------------------------
        // 選択されたエネルギータイプを取得
        // ----------------------------------------------------------------------
        private List<Enum.PokemonType> GetSelectedEnergyTypes()
        {
            var selectedTypes = new List<Enum.PokemonType>();
            for (int i = 0; i < energyTypeSelected.Count && i < Constants.AVAILABLE_ENERGY_TYPES.Length; i++)
            {
                if (energyTypeSelected[i])
                {
                    selectedTypes.Add(Constants.AVAILABLE_ENERGY_TYPES[i].type);
                }
            }
            return selectedTypes;
        }

        // ----------------------------------------------------------------------
        // 有効なカードIDリストを取得
        // ----------------------------------------------------------------------
        private List<string> GetValidCardIds()
        {
            return cardIds.Where(id => !string.IsNullOrEmpty(id.Trim())).Select(id => id.Trim()).ToList();
        }

        // ======================================================================
        // サンプルデッキ作成メソッド
        // ======================================================================

        // ----------------------------------------------------------------------
        // サンプルデッキ設定を作成
        // ----------------------------------------------------------------------
        private void CreateSampleDeckConfig()
        {
            var deckManager = FindFirstObjectByType<DeckManager>();
            if (deckManager == null)
            {
                EditorUtility.DisplayDialog(Constants.ERROR_TITLE, Constants.DECK_MANAGER_NOT_FOUND_MESSAGE, Constants.OK_BUTTON_TEXT);
                return;
            }

            try
            {
                // SerializedObjectを使用してInspectorの設定を更新
                var serializedObject = new SerializedObject(deckManager);
                var sampleDecksProperty = serializedObject.FindProperty(Constants.SAMPLE_DECKS_PROPERTY);
                
                // 新しいサンプルデッキエントリを追加
                sampleDecksProperty.arraySize++;
                var newElement = sampleDecksProperty.GetArrayElementAtIndex(sampleDecksProperty.arraySize - 1);
                
                // 基本情報を設定
                newElement.FindPropertyRelative(Constants.DECK_NAME_PROPERTY).stringValue = deckName;
                newElement.FindPropertyRelative(Constants.DECK_MEMO_PROPERTY).stringValue = deckMemo;
                newElement.FindPropertyRelative(Constants.MAX_CARDS_PROPERTY).intValue = maxCards;
                
                // エネルギータイプを設定
                var selectedTypes = GetSelectedEnergyTypes();
                var energyTypesProperty = newElement.FindPropertyRelative(Constants.ENERGY_TYPES_PROPERTY);
                energyTypesProperty.arraySize = selectedTypes.Count;
                for (int i = 0; i < selectedTypes.Count; i++)
                {
                    energyTypesProperty.GetArrayElementAtIndex(i).intValue = (int)selectedTypes[i];
                }
                
                // カードIDを設定
                var validCardIds = GetValidCardIds();
                var cardIdsProperty = newElement.FindPropertyRelative(Constants.SPECIFIC_CARD_IDS_PROPERTY);
                cardIdsProperty.arraySize = validCardIds.Count;
                for (int i = 0; i < validCardIds.Count; i++)
                {
                    cardIdsProperty.GetArrayElementAtIndex(i).stringValue = validCardIds[i];
                }
                
                // 変更を適用
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(deckManager);
                
                // 成功メッセージ
                EditorUtility.DisplayDialog(Constants.SUCCESS_TITLE, string.Format(Constants.SUCCESS_MESSAGE_FORMAT, deckName), Constants.OK_BUTTON_TEXT);
                
                // DeckManagerをハイライト表示
                Selection.activeObject = deckManager;
                EditorGUIUtility.PingObject(deckManager);
                
            }
            catch (System.Exception ex)
            {
                EditorUtility.DisplayDialog(Constants.ERROR_TITLE, string.Format(Constants.CREATION_ERROR_MESSAGE_FORMAT, ex.Message), Constants.OK_BUTTON_TEXT);
            }
        }

        // ======================================================================
        // ユーティリティメソッド
        // ======================================================================

        // ----------------------------------------------------------------------
        // フォームをクリア
        // ----------------------------------------------------------------------
        private void ClearForm()
        {
            if (EditorUtility.DisplayDialog(Constants.CLEAR_CONFIRM_TITLE, Constants.CLEAR_CONFIRM_MESSAGE, Constants.YES_BUTTON_TEXT, Constants.NO_BUTTON_TEXT))
            {
                deckName = "";
                deckMemo = "";
                maxCards = Constants.DEFAULT_MAX_CARDS;
                energyTypeSelected = new List<bool>(new bool[Constants.AVAILABLE_ENERGY_TYPES.Length]);
                cardIds = new List<string> { "" };
                
            }
        }
    }
}
