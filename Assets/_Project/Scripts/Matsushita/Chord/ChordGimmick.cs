using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ギミック：同時押し式。問題に合う2〜3個のボタンを、同時に押して答える（S1〜S5）。
/// 最初のボタンを押してから受付時間（ChordWindow）のあいだに押されたボタンを、爆弾本体がためて AnswerChord にまとめて渡す
/// </summary>
public class ChordGimmick : Gimmick
{
    private const int ButtonCount = 4;

    // 同じ種類を続けて出してよい回数（3回続けない → 2回まで）
    private const int MaxSameTypeInRow = 2;

    // 色混ぜの、難易度1で出すときの最低の段階（難易度1に設定されても、やさしい組だけ出す）
    private const int MinMixStage = 2;

    private const string DefaultDisplayName = "同時押し式";
    private const string DefaultDescription = "ボタンを同時に押せ";

    private static readonly string[] ColorNames = { "赤", "青", "黄", "緑" };

    // ボタンの色（InputDirection の並び：上キー＝赤、下キー＝青、左＝黄、右＝緑）
    private static readonly WireColor[] FixedColors = { WireColor.Red, WireColor.Blue, WireColor.Yellow, WireColor.Green };

    // 液晶（暗い背景）で読みやすい色。WireColor の並びと同じ
    private static readonly string[] ColorCodes = { "#FF5A5F", "#5C9DFF", "#FFD33D", "#3CE07F" };

    private static readonly string[] Symbols = { "○", "△", "□", "☆" };

    // 色混ぜの組。minStage の段階から出る。緑は「青と黄を混ぜる」ひっかけ（緑の線を1つ押すと不正解）
    private static readonly MixEntry[] Mixes =
    {
        new MixEntry("紫", "#C77DFF", WireColor.Red, WireColor.Blue, 2),
        new MixEntry("橙", "#FF9A2E", WireColor.Red, WireColor.Yellow, 2),
        new MixEntry("黄緑", "#B7E34A", WireColor.Yellow, WireColor.Green, 3),
        new MixEntry("水色", "#45D9E8", WireColor.Blue, WireColor.Green, 3),
        new MixEntry("茶色", "#C08A55", WireColor.Red, WireColor.Green, 3),
        new MixEntry("緑", "#3CE07F", WireColor.Blue, WireColor.Yellow, 4)
    };

    [SerializeField, Tooltip("試合画面の爆弾（ボタンの色と記号・数字をここに出す。テストシーンでは空でよい）")] private UiBombView _bombView;
    [SerializeField, Min(0.05f), Tooltip("同時押しの受付時間（秒）。キーボード・ゲームパッド用")] private float _windowSeconds = 0.2f;
    [SerializeField, Min(0.05f), Tooltip("同時押しの受付時間（秒）。マウスのクリック用（クリックは同時にできないので長め）")] private float _mouseWindowSeconds = 1f;
    [SerializeField, Range(0.7f, 1.6f), Tooltip("謎の重さ。COMの考える時間に掛ける")] private float _thinkWeight = 1.1f;

    // 難易度ごとに出す謎の種類（計画書の「難易度の段階」の表のとおり）
    [SerializeField] private ChordPuzzleType[] _stage1Types = { ChordPuzzleType.ColorPair };
    [SerializeField] private ChordPuzzleType[] _stage2Types = { ChordPuzzleType.ColorPair, ChordPuzzleType.ColorMix };
    [SerializeField] private ChordPuzzleType[] _stage3Types = { ChordPuzzleType.ColorMix, ChordPuzzleType.SameSymbol, ChordPuzzleType.SumTen };
    [SerializeField] private ChordPuzzleType[] _stage4Types = { ChordPuzzleType.ColorMix, ChordPuzzleType.SumTen, ChordPuzzleType.ExceptOne };

    // 方向ごとの線の色（InputDirection の並び）
    private readonly WireColor[] _colors = new WireColor[ButtonCount];

    // 方向ごとのボタンの文字（記号・数字。出さないときは空）
    private readonly string[] _labels = new string[ButtonCount];

    // 答えのボタンの方向
    private readonly List<InputDirection> _answers = new List<InputDirection>();

    private readonly List<InputDirection> _single = new List<InputDirection>();
    private string _questionText = string.Empty;
    private ChordPuzzleType _lastType;
    private int _sameTypeCount;

    /// <summary>
    /// ギミックの名前（Inspector で付けていなければ「同時押し式」）
    /// </summary>
    public override string DisplayName => string.IsNullOrEmpty(base.DisplayName) ? DefaultDisplayName : base.DisplayName;

    /// <summary>
    /// ギミックの一行説明（Inspector で付けていなければ「ボタンを同時に押せ」）
    /// </summary>
    public override string Description => string.IsNullOrEmpty(base.Description) ? DefaultDescription : base.Description;

    /// <summary>
    /// 液晶に出す問題文
    /// </summary>
    public override string QuestionText => _questionText;

    /// <summary>
    /// 答えのボタンの1つ目の方向（同時押しの答えは ChordAnswers にある）
    /// </summary>
    public override InputDirection NextAnswer => _answers.Count > 0 ? _answers[0] : InputDirection.Up;

    /// <summary>
    /// 謎の重さ
    /// </summary>
    public override float ThinkWeight => _thinkWeight;

    /// <summary>
    /// 同時押しの受付時間（キーボード・ゲームパッド）
    /// </summary>
    public override float ChordWindow => _windowSeconds;

    /// <summary>
    /// 同時押しの受付時間（マウスのクリック）
    /// </summary>
    public override float ChordMouseWindow => _mouseWindowSeconds;

    /// <summary>
    /// 同時押しの答え（CPUが一度に押す）
    /// </summary>
    public override IReadOnlyList<InputDirection> ChordAnswers => _answers;

    /// <summary>
    /// ラウンドの最初に呼ばれる。同じ種類の連続回数を数え直す
    /// </summary>
    public override void ResetRound()
    {
        _sameTypeCount = 0;
    }

    /// <summary>
    /// 爆弾を受け取るたびに呼ばれる。ボタンの色を置き、段階に合った謎を作る
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    public override void CreatePuzzle(int stage)
    {
        SetFixedColors();

        for (int i = 0; i < ButtonCount; i++)
        {
            _labels[i] = string.Empty;
        }

        _answers.Clear();

        ChordPuzzleType type = PickPuzzleType(stage);

        switch (type)
        {
            case ChordPuzzleType.ColorPair:
                CreateColorPair();
                break;
            case ChordPuzzleType.ColorMix:
                CreateColorMix(stage);
                break;
            case ChordPuzzleType.SameSymbol:
                CreateSameSymbol();
                break;
            case ChordPuzzleType.SumTen:
                CreateSumTen();
                break;
            case ChordPuzzleType.ExceptOne:
                CreateExceptOne();
                break;
        }

        ShowPuzzle();
        Debug.Log($"[同時押し] 段階{stage}（{type}）：{_questionText}（答え：{string.Join("+", _answers)}）");
    }

    /// <summary>
    /// 受付時間のあいだに押されたボタンをまとめて判定する。
    /// 答えにないボタンが混ざれば Wrong、答えがそろえば Correct、足りないうちは受付時間が終わるまで Continue
    /// </summary>
    /// <param name="pressed">ここまでに押された方向</param>
    /// <param name="isWindowClosed">受付時間が終わったか</param>
    /// <returns>入力した結果</returns>
    public override AnswerResult AnswerChord(IReadOnlyList<InputDirection> pressed, bool isWindowClosed)
    {
        if (_answers.Count == 0)
        {
            Debug.LogWarning("[同時押し] CreatePuzzle の前に AnswerChord が呼ばれました");
            return AnswerResult.Wrong;
        }

        for (int i = 0; i < pressed.Count; i++)
        {
            if (!_answers.Contains(pressed[i]))
            {
                return AnswerResult.Wrong;
            }
        }

        if (pressed.Count >= _answers.Count)
        {
            return AnswerResult.Correct;
        }

        return isWindowClosed ? AnswerResult.Wrong : AnswerResult.Continue;
    }

    /// <summary>
    /// 1つだけ押されたときの入口（爆弾本体は同時押しのとき AnswerChord を使うので、テスト用）
    /// </summary>
    /// <param name="direction">押された方向</param>
    /// <returns>入力した結果</returns>
    public override AnswerResult Answer(InputDirection direction)
    {
        _single.Clear();
        _single.Add(direction);
        return AnswerChord(_single, true);
    }

    /// <summary>
    /// 4色を上下左右に、いつも同じ並びで置く（爆弾のコードの絵は色ごとに位置が決まっているので、入れ替えない）
    /// </summary>
    private void SetFixedColors()
    {
        for (int i = 0; i < ButtonCount; i++)
        {
            _colors[i] = FixedColors[i];
        }
    }

    /// <summary>
    /// S1 色指定：違う2色を選び、その2つを答えにする
    /// </summary>
    private void CreateColorPair()
    {
        WireColor first = (WireColor)Random.Range(0, ButtonCount);
        WireColor second = (WireColor)(((int)first + Random.Range(1, ButtonCount)) % ButtonCount);

        _questionText = $"{Colored(first)}と{Colored(second)}を同時に押せ";
        AddAnswer(first);
        AddAnswer(second);
    }

    /// <summary>
    /// S2 色混ぜ：「紫をつくれ」なら赤と青。難易度が上がると組が増え、難易度4では緑（青と黄）のひっかけも出る
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    private void CreateColorMix(int stage)
    {
        int limit = Mathf.Max(stage, MinMixStage);
        List<MixEntry> pool = new List<MixEntry>();

        foreach (MixEntry mix in Mixes)
        {
            if (mix.MinStage <= limit)
            {
                pool.Add(mix);
            }
        }

        MixEntry picked = pool[Random.Range(0, pool.Count)];

        _questionText = $"<color={picked.Code}>{picked.Name}</color>をつくれ";
        AddAnswer(picked.First);
        AddAnswer(picked.Second);
    }

    /// <summary>
    /// S3 同じ記号：1組だけ同じ記号、残り2つは別の記号にして、同じ2つを答えにする
    /// </summary>
    private void CreateSameSymbol()
    {
        int[] order = { 0, 1, 2, 3 };
        Shuffle(order);

        // order[0] を2回使い、order[1]・order[2] を1回ずつ使う
        string[] arrangement = { Symbols[order[0]], Symbols[order[0]], Symbols[order[1]], Symbols[order[2]] };

        for (int i = arrangement.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (arrangement[i], arrangement[j]) = (arrangement[j], arrangement[i]);
        }

        for (int i = 0; i < ButtonCount; i++)
        {
            _labels[i] = arrangement[i];

            if (arrangement[i] == Symbols[order[0]])
            {
                _answers.Add((InputDirection)i);
            }
        }

        _questionText = "同じ記号の2つを同時に押せ";
    }

    /// <summary>
    /// S4 足して10：1〜9から4つ選び、足して10になる組がちょうど1組のものだけ出す。その組を答えにする
    /// </summary>
    private void CreateSumTen()
    {
        int[] numbers = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        int firstIndex = -1;
        int secondIndex = -1;

        // 条件に合う選び方は十分にあるので、数回で決まる。念のため回数に上限を付ける
        for (int attempt = 0; attempt < 200; attempt++)
        {
            Shuffle(numbers);

            int pairCount = 0;

            for (int i = 0; i < ButtonCount; i++)
            {
                for (int j = i + 1; j < ButtonCount; j++)
                {
                    if (numbers[i] + numbers[j] == 10)
                    {
                        pairCount++;
                        firstIndex = i;
                        secondIndex = j;
                    }
                }
            }

            if (pairCount == 1)
            {
                break;
            }
        }

        for (int i = 0; i < ButtonCount; i++)
        {
            _labels[i] = numbers[i].ToString();
        }

        _answers.Add((InputDirection)firstIndex);
        _answers.Add((InputDirection)secondIndex);
        _questionText = "足して10になる2つを同時に押せ";
    }

    /// <summary>
    /// S5 3つ同時：ある色のボタンだけ除いた、残り3つを答えにする
    /// </summary>
    private void CreateExceptOne()
    {
        WireColor except = (WireColor)Random.Range(0, ButtonCount);

        for (int i = 0; i < ButtonCount; i++)
        {
            if (_colors[i] != except)
            {
                _answers.Add((InputDirection)i);
            }
        }

        _questionText = $"{Colored(except)}以外を全部同時に押せ";
    }

    /// <summary>
    /// その色のボタンがある方向を、答えに加える
    /// </summary>
    /// <param name="color">答えにする色</param>
    private void AddAnswer(WireColor color)
    {
        for (int i = 0; i < ButtonCount; i++)
        {
            if (_colors[i] == color)
            {
                _answers.Add((InputDirection)i);
                return;
            }
        }
    }

    /// <summary>
    /// 色の名前を、その色の文字にして返す
    /// </summary>
    /// <param name="color">色</param>
    /// <returns>色付きの色名</returns>
    private static string Colored(WireColor color)
    {
        return $"<color={ColorCodes[(int)color]}>{ColorNames[(int)color]}</color>";
    }

    /// <summary>
    /// 段階に合う謎の種類を均等に選ぶ。同じ種類が3回続かないようにする
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    /// <returns>選んだ謎の種類</returns>
    private ChordPuzzleType PickPuzzleType(int stage)
    {
        List<ChordPuzzleType> candidates = new List<ChordPuzzleType>(GetStageTypes(stage));

        // 2回続いていたら外す（候補が1つしかない段階1は外せないのでそのまま）
        if (_sameTypeCount >= MaxSameTypeInRow && candidates.Count > 1)
        {
            candidates.Remove(_lastType);
        }

        ChordPuzzleType picked = candidates[Random.Range(0, candidates.Count)];

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
    private ChordPuzzleType[] GetStageTypes(int stage)
    {
        ChordPuzzleType[] types;

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
            Debug.LogWarning($"[同時押し] 段階{stage}の設定がないので、S1（色指定）を出します");
            return new[] { ChordPuzzleType.ColorPair };
        }

        return types;
    }

    /// <summary>
    /// ボタンの色・文字を試合画面の爆弾に出す（置いていなければ飛ばす）
    /// </summary>
    private void ShowPuzzle()
    {
        if (_bombView == null)
        {
            return;
        }

        for (int i = 0; i < ButtonCount; i++)
        {
            InputDirection direction = (InputDirection)i;
            _bombView.SetWire(direction, ToUiWireColor(_colors[i]), UiWireState.Normal);

            // 記号・数字は、ボタンの真ん中に大きく出す（空なら色名の頭文字に戻る）
            _bombView.SetButtonLabel(direction, _labels[i]);
            _bombView.SetWireSymbol(direction, string.Empty);
        }

        _bombView.SetLamp(false);
        _bombView.SetInput(string.Empty);
    }

    /// <summary>
    /// 配線の色を、試合画面の爆弾の色に変える
    /// </summary>
    /// <param name="color">配線の色</param>
    /// <returns>試合画面の爆弾の色</returns>
    private static UiWireColor ToUiWireColor(WireColor color)
    {
        switch (color)
        {
            case WireColor.Red:
                return UiWireColor.Red;
            case WireColor.Blue:
                return UiWireColor.Blue;
            case WireColor.Yellow:
                return UiWireColor.Yellow;
            default:
                return UiWireColor.Green;
        }
    }

    /// <summary>
    /// 配列の中身をランダムな順に入れ替える
    /// </summary>
    /// <param name="array">入れ替える配列</param>
    private static void Shuffle(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
    }

    /// <summary>
    /// 色混ぜの1つの組（混ぜてできる色の名前と色、材料の2色、出始める難易度）
    /// </summary>
    private sealed class MixEntry
    {
        public MixEntry(string name, string code, WireColor first, WireColor second, int minStage)
        {
            Name = name;
            Code = code;
            First = first;
            Second = second;
            MinStage = minStage;
        }

        public string Name { get; }

        public string Code { get; }

        public WireColor First { get; }

        public WireColor Second { get; }

        public int MinStage { get; }
    }
}
