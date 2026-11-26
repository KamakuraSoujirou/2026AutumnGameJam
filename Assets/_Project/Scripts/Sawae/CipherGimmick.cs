using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ギミック2：暗号。4つのボタンを、決まった順番どおりに押せたらクリア。
/// 間違えたら最初からやり直し。今は結果を Console に出すだけ
/// </summary>
public class CipherGimmick : MonoBehaviour
{
    private const int ButtonCount = 4;

    // 押す対象の4つのボタン(並び順がそのまま番号0〜3になる)
    [SerializeField] private Button[] _buttons = new Button[ButtonCount];

    // 正解の押す順番。ボタン番号を並べたもの（例：{2, 0, 3, 1}）
    private int[] _answerOrder = new int[ButtonCount];
    private int _enteredCount;

    /// <summary>
    /// 押す順番を表す文字列（ボタンのオブジェクト名で表示）
    /// </summary>
    public string QuestionText
    {
        get
        {
            string[] names = new string[_answerOrder.Length];

            for (int i = 0; i < _answerOrder.Length; i++)
            {
                names[i] = _buttons[_answerOrder[i]].name;
            }

            return string.Join(" → ", names);
        }
    }

    /// <summary>
    /// 4つのボタン（他のオブジェクト）に押されたときの処理を登録し、最初の問題を出す
    /// </summary>
    private void Start()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            int index = i;
            _buttons[i].onClick.AddListener(() => Press(index));
        }

        CreatePuzzle();
    }

    /// <summary>
    /// 4つのボタンを並べ替えて、押す順番を決める
    /// </summary>
    public void CreatePuzzle()
    {
        for (int i = 0; i < _answerOrder.Length; i++)
        {
            _answerOrder[i] = i;
        }

        Shuffle(_answerOrder);
        _enteredCount = 0;

        Debug.Log($"[暗号] 問題：{QuestionText} の順に押す");
    }

    /// <summary>
    /// ボタンが押されたときに呼ばれる。順番どおりなら進み、4つそろえばクリア、間違えたら最初から
    /// </summary>
    /// <param name="buttonIndex">押されたボタンの番号（_buttons の順、0〜3）</param>
    public void Press(int buttonIndex)
    {
        string buttonName = _buttons[buttonIndex].name;

        if (buttonIndex != _answerOrder[_enteredCount])
        {
            _enteredCount = 0;
            Debug.Log($"[暗号] {buttonName} → 間違い（最初からやり直し）");

            // TODO: 間違えたときのペナルティ（後で書く）
            // ApplyPenalty();

            return;
        }

        _enteredCount++;

        if (_enteredCount >= _answerOrder.Length)
        {
            Debug.Log($"[暗号] {buttonName} → クリア");
            CreatePuzzle();
            return;
        }

        Debug.Log($"[暗号] {buttonName} → 正解（{_enteredCount}/{_answerOrder.Length}）");
    }

    // TODO: ペナルティ（後で書く）
    // /// <summary>
    // /// 間違えたときのペナルティ
    // /// </summary>
    // private void ApplyPenalty()
    // {
    // }

    /// <summary>
    /// 配列の中身をランダムな順に入れ替える
    /// </summary>
    /// <param name="array">入れ替える配列</param>
    private static void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            int temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }
}