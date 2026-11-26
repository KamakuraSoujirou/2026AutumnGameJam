using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 爆弾モジュール全体の制御
/// </summary>
public class BombManager : MonoBehaviour
{
    [SerializeField, Tooltip("爆発する時間")] private float _explosionTime = 10f;

    [SerializeField, Tooltip("問題テキスト")] private TMP_Text _questionTemPlate;

    [SerializeField, Tooltip("問題文")] private string[] _questionTextes;
    private int _turn = 1;   //現在のターン
    private int IsContinueCount = 0;        //コンティニュー出来る回数

    private float _currentExplosionTime;
    private float _pernalty = -2f;
    private bool _IsStun;

    public float Explotion => _currentExplosionTime;
    public float PernaltyTime => _pernalty;

    public bool IsStun => _IsStun;

    public string[] QuestionText => _questionTextes;





    private Gimmick gimmick;

     public void StartRound(int stage)
    { 
        _currentExplosionTime = _explosionTime;   // 時間の初期化はここ
        _IsStun = false;

        gimmick.ResetRound();                   // 1. ギミックの状態を初期化
        gimmick.CreatePuzzle(stage);            // 2. 問題を作る
        _questionTemPlate.text = _questionTextes[stage];
    }

    // Gimmick・InputDirection・AnswerResult は Assets/_Project/Scripts/Kamakura/Core/ に移動

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        gimmick = GetComponent<Gimmick>();
    }

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
        }
    }

    private void Question(int s)
    {

        #region float
        float _currentTurn = _turn;
        #endregion


        if (QuestioAnswer(s,IsContinueCount) is not AnswerResult answer) return;
        switch (answer)
        {
            case AnswerResult.Continue:  //
                {
                        Debug.LogWarning($"乱数{s} コンティニューしたのでモジュールの処理はスキップした。");
                        break;
                }
            case AnswerResult.Correct:   //正解
                {
                    IsContinueCount += 1;
                    _currentTurn += 1;
                    Debug.Log($"乱数{s} コンティニュー回数{IsContinueCount} 正解、モジュールが初期化された");
                    break;
                }
            case AnswerResult.Wrong:   //不正解
                {
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
        return null;
    }
    /// <summary>
    /// 問題の正誤判定
    /// </summary>
    /// <returns></returns>
    private AnswerResult? QuestioAnswer(int n,int s)
    {
        if (n <= 5 && s >= 5) return AnswerResult.Continue;
        if (n <= 5 && s <= 4) return AnswerResult.Wrong;
        if (n >= 6) return AnswerResult.Correct;
        return null;
    }
}
