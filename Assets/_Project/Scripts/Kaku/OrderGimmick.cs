using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ギミック3：順番スイッチ。上下左右の4つのスイッチを、問題に合う順に全部押す（C1〜C5）。
/// 手作りの組（C2〜C4）は Resources/Puzzles/OrderSwitch.json から読む
/// </summary>
public class OrderGimmick : Gimmick
{
    private const int SwitchCount = 4;

    // 同じ種類を続けて出してよい回数（3回続けない → 2回まで）
    private const int MaxSameTypeInRow = 2;

    // C1 で使う数字の範囲
    private const int MinNumber = 1;
    private const int MaxNumber = 99;

    private const string NumberQuestionText = "数字の小さい順に押せ";
    private const string ReverseSuffix = "（ただし逆順）";
    private const string EmptySlotText = "_";

    // C5 逆順の元にする種類
    private static readonly OrderPuzzleType[] ReverseBaseTypes =
    {
        OrderPuzzleType.Number,
        OrderPuzzleType.Alphabet,
        OrderPuzzleType.Time,
        OrderPuzzleType.Size
    };

    // 各スイッチの文字を出すテキスト（InputDirection の並び：Up, Down, Left, Right の順）
    [SerializeField] private Text[] _switchLabels = new Text[SwitchCount];

    // 押した順の番号を出すテキスト（並びは上と同じ）
    [SerializeField] private Text[] _orderLabels = new Text[SwitchCount];

    // 押したスイッチの文字を、押した順に出すテキスト
    [SerializeField] private Text _enteredText;

    // 試合画面の爆弾（試合では、スイッチの文字・押した順の番号・入力済みの文字をここに出す。テストシーンでは空でよい）
    [SerializeField] private UiBombView _bombView;

    // 難易度ごとに出す謎の種類（Notion「難易度の段階」の表のとおり）
    [SerializeField] private OrderPuzzleType[] _stage1Types = { OrderPuzzleType.Number };
    [SerializeField] private OrderPuzzleType[] _stage2Types = { OrderPuzzleType.Number, OrderPuzzleType.Time };
    [SerializeField] private OrderPuzzleType[] _stage3Types = { OrderPuzzleType.Alphabet, OrderPuzzleType.Time, OrderPuzzleType.Size };
    [SerializeField] private OrderPuzzleType[] _stage4Types = { OrderPuzzleType.Alphabet, OrderPuzzleType.Size, OrderPuzzleType.Reverse };

    // 各スイッチに出している文字（InputDirection の並び）
    private readonly string[] _labels = new string[SwitchCount];

    // 正解の順に並べたスイッチの方向
    private readonly List<InputDirection> _directionSet = new List<InputDirection>();

    private PuzzleDatabase _database;
    private string _questionText = string.Empty;
    private int _clearCount;
    private OrderPuzzleType _lastType;
    private int _sameTypeCount;

    /// <summary>
    /// 液晶に出す問題文
    /// </summary>
    public override string QuestionText => _questionText;

    /// <summary>
    /// 次に押すと正解の方向（CPUが使う）
    /// </summary>
    public override InputDirection NextAnswer
    {
        get
        {
            if (_directionSet.Count == 0)
            {
                return InputDirection.Up;
            }

            return _directionSet[Mathf.Min(_clearCount, _directionSet.Count - 1)];
        }
    }

    /// <summary>
    /// 手作りの組を読んだデータ（初めて使うときに読む）
    /// </summary>
    private PuzzleDatabase Database
    {
        get
        {
            if (_database == null)
            {
                _database = new PuzzleDatabase(PuzzleDatabase.OrderSwitchPath);
            }

            return _database;
        }
    }

    /// <summary>
    /// ラウンドの最初に呼ばれる。押した数と、同じ種類の連続回数をリセットする
    /// </summary>
    public override void ResetRound()
    {
        _clearCount = 0;
        _sameTypeCount = 0;
    }

    /// <summary>
    /// 爆弾を受け取るたびに呼ばれる。段階に合った謎を選び、4つの言葉をスイッチにランダムに割り振る
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    public override void CreatePuzzle(int stage)
    {
        OrderPuzzleType type = PickPuzzleType(stage);
        bool isReverse = type == OrderPuzzleType.Reverse;

        if (isReverse)
        {
            type = ReverseBaseTypes[UnityEngine.Random.Range(0, ReverseBaseTypes.Length)];
        }

        // 正解の順に並んだ4つの言葉と、問題文を作る
        string[] orderedItems = CreateItems(type, out _questionText);

        if (isReverse)
        {
            Array.Reverse(orderedItems);
            _questionText += ReverseSuffix;
        }

        // 4つの言葉を、上下左右のスイッチにランダムに割り振る
        InputDirection[] directions = { InputDirection.Up, InputDirection.Down, InputDirection.Left, InputDirection.Right };
        Shuffle(directions);

        _directionSet.Clear();

        for (int i = 0; i < SwitchCount; i++)
        {
            _labels[(int)directions[i]] = orderedItems[i];
            _directionSet.Add(directions[i]);
        }

        _clearCount = 0;
        ShowSwitches();
        Debug.Log($"[順番] 段階{stage}（{(isReverse ? "Reverse+" : string.Empty)}{type}）：{_questionText}（{string.Join(" → ", orderedItems)}）");
    }

    /// <summary>
    /// ボタンが押されたときに呼ばれる。途中まで合っていれば Continue、4つ押し終えたら Correct、違えば Wrong
    /// </summary>
    /// <param name="direction">押されたスイッチの方向</param>
    /// <returns>入力した結果</returns>
    public override AnswerResult Answer(InputDirection direction)
    {
        if (_directionSet.Count == 0)
        {
            Debug.LogWarning("[順番] CreatePuzzle の前に Answer が呼ばれました");
            return AnswerResult.Continue;
        }

        // 押したスイッチはもう押せない（何も起きない）
        if (IsPressed(direction))
        {
            return AnswerResult.Continue;
        }

        if (direction != _directionSet[_clearCount])
        {
            // 間違えたら、押した分を消して最初から（時間減少とスタンは爆弾本体が行う）
            _clearCount = 0;
            ShowSwitches();
            return AnswerResult.Wrong;
        }

        _clearCount++;
        ShowSwitches();

        if (_clearCount >= SwitchCount)
        {
            return AnswerResult.Correct;
        }

        return AnswerResult.Continue;
    }

    /// <summary>
    /// 種類に合った4つの言葉を、正解の順に並べて返す
    /// </summary>
    /// <param name="type">謎の種類（C1〜C4）</param>
    /// <param name="questionText">液晶に出す問題文</param>
    /// <returns>正解の順に並んだ4つの言葉</returns>
    private string[] CreateItems(OrderPuzzleType type, out string questionText)
    {
        string puzzleType = GetPuzzleTypeId(type);

        if (puzzleType != null)
        {
            PuzzleData puzzle = Database.Draw(puzzleType);

            if (puzzle != null)
            {
                questionText = puzzle.Text;
                return (string[])puzzle.Items.Clone();
            }

            Debug.LogWarning($"[順番] {puzzleType} の組が読めないので、C1（数字）を出します");
        }

        questionText = NumberQuestionText;
        return CreateNumbers();
    }

    /// <summary>
    /// C1：1〜99から違う数字を4つ選び、小さい順に並べる
    /// </summary>
    /// <returns>小さい順に並んだ4つの数字</returns>
    private string[] CreateNumbers()
    {
        List<int> numberSet = new List<int>();

        while (numberSet.Count < SwitchCount)
        {
            int number = UnityEngine.Random.Range(MinNumber, MaxNumber + 1);

            if (!numberSet.Contains(number))
            {
                numberSet.Add(number);
            }
        }

        numberSet.Sort();

        string[] items = new string[SwitchCount];

        for (int i = 0; i < SwitchCount; i++)
        {
            items[i] = numberSet[i].ToString();
        }

        return items;
    }

    /// <summary>
    /// 謎の種類を、JSON に書いてある種類の名前（C2〜C4）にする
    /// </summary>
    /// <param name="type">謎の種類</param>
    /// <returns>C2・C3・C4 のどれか。JSON を使わない種類は null</returns>
    private string GetPuzzleTypeId(OrderPuzzleType type)
    {
        switch (type)
        {
            case OrderPuzzleType.Alphabet:
                return "C2";
            case OrderPuzzleType.Time:
                return "C3";
            case OrderPuzzleType.Size:
                return "C4";
            default:
                return null;
        }
    }

    /// <summary>
    /// 段階に合う謎の種類を均等に選ぶ。同じ種類が3回続かないようにする
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    /// <returns>選んだ謎の種類</returns>
    private OrderPuzzleType PickPuzzleType(int stage)
    {
        List<OrderPuzzleType> candidates = new List<OrderPuzzleType>(GetStageTypes(stage));

        // 2回続いていたら外す（候補が1つしかない段階1は外せないのでそのまま）
        if (_sameTypeCount >= MaxSameTypeInRow && candidates.Count > 1)
        {
            candidates.Remove(_lastType);
        }

        OrderPuzzleType picked = candidates[UnityEngine.Random.Range(0, candidates.Count)];

        if (_sameTypeCount > 0 && picked == _lastType)
        {
            _sameTypeCount++;
        }
        else
        {
            _sameTypeCount = 1;
        }

        _lastType = picked;
        return picked;
    }

    /// <summary>
    /// 段階に合う謎の種類の一覧を返す
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    /// <returns>出してよい謎の種類</returns>
    private OrderPuzzleType[] GetStageTypes(int stage)
    {
        OrderPuzzleType[] types;

        switch (stage)
        {
            case 1:
                types = _stage1Types;
                break;
            case 2:
                types = _stage2Types;
                break;
            case 3:
                types = _stage3Types;
                break;
            case 4:
                types = _stage4Types;
                break;
            default:
                types = null;
                break;
        }

        if (types == null || types.Length == 0)
        {
            Debug.LogWarning($"[順番] 段階{stage}の設定がないので、C1（数字）を出します");
            return new[] { OrderPuzzleType.Number };
        }

        return types;
    }

    /// <summary>
    /// そのスイッチをもう押したかを調べる
    /// </summary>
    /// <param name="direction">スイッチの方向</param>
    /// <returns>押していれば true</returns>
    private bool IsPressed(InputDirection direction)
    {
        return GetPressedOrder(direction) > 0;
    }

    /// <summary>
    /// そのスイッチを何番目に押したかを返す
    /// </summary>
    /// <param name="direction">スイッチの方向</param>
    /// <returns>押した順の番号（1〜4）。まだ押していなければ 0</returns>
    private int GetPressedOrder(InputDirection direction)
    {
        for (int i = 0; i < _clearCount; i++)
        {
            if (_directionSet[i] == direction)
            {
                return i + 1;
            }
        }

        return 0;
    }

    /// <summary>
    /// スイッチの文字、押した順の番号、入力済みの文字を表示する（置いていないものは飛ばす）
    /// </summary>
    private void ShowSwitches()
    {
        for (int i = 0; i < SwitchCount; i++)
        {
            InputDirection direction = (InputDirection)i;
            int order = GetPressedOrder(direction);
            string orderText = order > 0 ? order.ToString() : string.Empty;

            if (i < _switchLabels.Length && _switchLabels[i] != null)
            {
                _switchLabels[i].text = _labels[i];
            }

            if (i < _orderLabels.Length && _orderLabels[i] != null)
            {
                _orderLabels[i].text = orderText;
            }

            if (_bombView != null)
            {
                _bombView.SetButtonLabel(direction, _labels[i]);
                _bombView.SetWireSymbol(direction, orderText);
            }
        }

        string[] slots = new string[SwitchCount];

        for (int i = 0; i < SwitchCount; i++)
        {
            slots[i] = i < _clearCount ? _labels[(int)_directionSet[i]] : EmptySlotText;
        }

        string entered = string.Join(" ", slots);

        if (_enteredText != null)
        {
            _enteredText.text = entered;
        }

        if (_bombView != null)
        {
            _bombView.SetInput(entered);
        }
    }

    /// <summary>
    /// 配列の中身をランダムな順に入れ替える
    /// </summary>
    /// <param name="array">入れ替える配列</param>
    private static void Shuffle(InputDirection[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }
}
