using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 爆弾モジュール全体の制御
/// </summary>
public class BombManager : MonoBehaviour
{
    [Header("")]
    [SerializeField, Tooltip("問題文")] private TMP_Text _questions;
    [SerializeField, Tooltip("問題文の内容")] private string[] _questionTextes;
    [SerializeField, Tooltip("ボタン")] private Button[] _buttons;

    private int _turn = 1;   //現在のターン
    private int IsContinueCount = 0;        //コンティニュー出来る回数
    private Coroutine _coroutine;



    private float _explosionTime = 10f;   //爆発する時間
    private float _pernaltyTime = -2f;    //ペナルティ
    private bool IsStan;   //スタンした時の真偽

    /// <summary>
    /// 方向の入力(CPU対戦時)    //完了済
    /// </summary>
    public enum InputDirection { Up, Down, Left, Right,Q}

    /// <summary>
    /// 入力した結果             //ほぼ
    /// </summary>
    public enum AnswerResult { Continue, Correct, Wrong }

    /// <summary>
    /// すべてのギミックの元になるクラス
    /// </summary>
    public abstract class Gimmick : MonoBehaviour
    {
        /// <summary>液晶に出す問題文</summary>
        public abstract string QuestionText { get; }
        /// <summary>次に押すと正解の方向（CPUが使う）</summary>
        public abstract InputDirection NextAnswer { get; }
        /// <summary>ラウンドの最初に呼ばれる</summary>
        public virtual void ResetRound()
        {

        }

        /// <summary>爆弾を受け取るたびに呼ばれる。stageは難易度の1〜4</summary>
        public abstract void CreatePuzzle(int stage);

        /// <summary>ボタンが押されたときに呼ばれる</summary>
        public abstract AnswerResult Answer(InputDirection direction);

        public class QuestionResult : Gimmick
        {
            public override string QuestionText
            {
                get
                {
                    return QuestionText;
                }
            }

            public override InputDirection NextAnswer => throw new System.NotImplementedException();

            public override AnswerResult Answer(InputDirection direction)
            {
                throw new System.NotImplementedException();
            }

            public override void CreatePuzzle(int stage)
            {
               // stage = 
                
            }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created


    void Start()
    {
        
    }
    void Update()
    {
        KeyBoardCheck();

    }
    /// <summary>
    /// 入力したキーに応じて、乱数を生成
    /// 生成した乱数の大きさによってメソッドを実装する
    /// </summary>
    private void KeyBoardCheck()
    {
        if (InputResult() is not InputDirection input) return;
        switch (input)
        {
            case InputDirection.Up:
                {
                    int n = Random.Range(1, 10);
                    Debug.Log($"上矢印を検出");
                    Question(n);
                    break;
                }
            case InputDirection.Down:
                {
                    int n = Random.Range(1, 10);
                    Debug.Log($"下矢印を検出");
                    Question(n);
                    break;
                }
            case InputDirection.Left:
                {
                    int n = Random.Range(1, 10);
                    Debug.Log($"左矢印を検出");
                    Question(n);
                    break;
                }
            case InputDirection.Right:
                {
                    int n = Random.Range(1, 10);
                    Debug.Log("右矢印を検出");
                    Question(n);
                    break;
                }
            case InputDirection Q:
                {
                    Debug.Log("Qキーを検出");
                    Question(IsContinueCount);
                }
                break;
        }
    }

    private void Question(int s)
    {

        #region float
        float _currentTime = _explosionTime;
        float _currentTurn = _turn;
        _currentTime -= Mathf.Floor(Time.deltaTime);
        #endregion


        if (QuestioAnswer(s) is not AnswerResult answer) return;
        switch (answer)
        {
            case AnswerResult.Continue:  //
                {
                        Debug.LogWarning($"コンティニューしたのでモジュールの処理はスキップした。");
                        break;
                }
            case AnswerResult.Correct:   //正解
                {
                    IsContinueCount += 1;
                    _currentTurn += 1;
                    _currentTime = _explosionTime;
                    Debug.Log($"乱数{s} コンティニュー回数{IsContinueCount} 正解、モジュールが初期化された");
                    break;
                }
            case AnswerResult.Wrong:   //不正解
                {
                    _currentTime -= _pernaltyTime;
                    Debug.LogError($"乱数{s} 不正解...モジュール処理が加速した");
                    break;
                }
        }
    }


    /// <summary>
    /// 入力の判定
    /// 完了済み
    /// </summary>
    /// <returns></returns>
    private InputDirection? InputResult()
    {
        if (Keyboard.current.upArrowKey.wasPressedThisFrame) return InputDirection.Up;
        if (Keyboard.current.downArrowKey.wasPressedThisFrame) return InputDirection.Down;
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) return InputDirection.Left;
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) return InputDirection.Right;
        if (Keyboard.current.qKey.wasPressedThisFrame && IsContinueCount >= 5) return InputDirection.Q;
        return null;
    }
    /// <summary>
    /// 問題の正誤判定
    /// </summary>
    /// <returns></returns>
    private AnswerResult? QuestioAnswer(int n)
    {
        if (n >= 5) return AnswerResult.Continue;
        if (n <= 5) return AnswerResult.Wrong;
        if (n >= 6) return AnswerResult.Correct;
        return null;
    }
}
