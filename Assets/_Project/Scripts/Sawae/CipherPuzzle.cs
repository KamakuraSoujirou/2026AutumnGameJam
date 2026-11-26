using System;
using System.Collections.Generic;

/// <summary>
/// 暗号の謎1問ぶん（問題文・ボタンの文字・正解）。
/// Create() に種類を渡すと、B1〜B5 の謎を作って返す
/// </summary>
public class CipherPuzzle
{
    /// <summary>
    /// 答えの長さ（3文字）
    /// </summary>
    public const int AnswerLength = 3;

    // B3 計算で使う数字の範囲（ボタンは1〜4）
    private const int MinNumber = 1;
    private const int MaxNumber = 4;

    // 使う方向（InputDirection の並び：Up, Down, Left, Right）
    private static readonly InputDirection[] AllDirections = (InputDirection[])Enum.GetValues(typeof(InputDirection));

    // 各方向のボタンの記号（InputDirection の並びと同じ順：Up＝○, Down＝△, Left＝□, Right＝☆）
    private static readonly string[] SymbolLabels = { "○", "△", "□", "☆" };

    // B3 計算のときのボタンの文字（Up＝1, Down＝2, Left＝3, Right＝4）
    private static readonly string[] NumberLabels = { "1", "2", "3", "4" };

    // B2 対応表に使うひらがなの候補（ここに足すと種類が増える）
    private static readonly string[] KanaPool = { "あ", "か", "さ", "た", "な", "は", "ま", "や" };

    // B5 数える：多い順に並べたときの個数（1位が3個、2位が2個、3位が1個）
    private static readonly int[] CountOrder = { 3, 2, 1 };

    /// <summary>
    /// 謎を作る（外からは Create() を使う）
    /// </summary>
    /// <param name="questionText">液晶に出す問題文</param>
    /// <param name="buttonLabels">各方向のボタンに表示する文字</param>
    /// <param name="answers">正解の方向（入力する順）</param>
    private CipherPuzzle(string questionText, string[] buttonLabels, InputDirection[] answers)
    {
        QuestionText = questionText;
        ButtonLabels = buttonLabels;
        Answers = answers;
    }

    /// <summary>
    /// 液晶に出す問題文
    /// </summary>
    public string QuestionText { get; }

    /// <summary>
    /// 各方向のボタンに表示する文字（InputDirection の並びと同じ順）
    /// </summary>
    public string[] ButtonLabels { get; }

    /// <summary>
    /// 正解の方向を、入力する順に並べたもの
    /// </summary>
    public InputDirection[] Answers { get; }

    /// <summary>
    /// 種類に合った謎を作る
    /// </summary>
    /// <param name="type">謎の種類（B1〜B5）</param>
    /// <returns>作った謎</returns>
    public static CipherPuzzle Create(CipherPuzzleType type)
    {
        switch (type)
        {
            case CipherPuzzleType.Same:
                return CreateSame();
            case CipherPuzzleType.Table:
                return CreateTable();
            case CipherPuzzleType.Calc:
                return CreateCalc();
            case CipherPuzzleType.Reverse:
                return CreateReverse();
            case CipherPuzzleType.Count:
                return CreateCount();
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, "未対応の謎の種類です");
        }
    }

    /// <summary>
    /// 方向に対応するボタンの文字を返す
    /// </summary>
    /// <param name="direction">方向</param>
    /// <returns>そのボタンに表示している文字</returns>
    public string GetLabel(InputDirection direction)
    {
        return ButtonLabels[(int)direction];
    }

    // ============================================================
    // B1〜B5 の作り方
    // ============================================================

    /// <summary>
    /// B1 そのまま：「△○☆」をそのまま入力する
    /// </summary>
    /// <returns>作った謎</returns>
    private static CipherPuzzle CreateSame()
    {
        InputDirection[] answers = CreateRandomDirections();
        string question = $"「{ToSymbolText(answers)}」をそのまま入力";
        return new CipherPuzzle(question, CreateSymbolLabels(), answers);
    }

    /// <summary>
    /// B2 対応表：「あ＝○ か＝△ さ＝□ た＝☆」と「さかた」→ □△☆
    /// </summary>
    /// <returns>作った謎</returns>
    private static CipherPuzzle CreateTable()
    {
        string[] kanaList = (string[])KanaPool.Clone();
        Shuffle(kanaList);

        // kanaList[i] ↔ tableDirections[i] の記号、という対応表
        InputDirection[] tableDirections = CreateShuffledDirections();

        string[] tableEntries = new string[tableDirections.Length];
        for (int i = 0; i < tableDirections.Length; i++)
        {
            tableEntries[i] = $"{kanaList[i]}＝{SymbolLabels[(int)tableDirections[i]]}";
        }

        // 出題する言葉：表の4つのうち3つを、ランダムな順で使う
        int[] pickOrder = { 0, 1, 2, 3 };
        Shuffle(pickOrder);

        string word = string.Empty;
        InputDirection[] answers = new InputDirection[AnswerLength];
        for (int i = 0; i < AnswerLength; i++)
        {
            word += kanaList[pickOrder[i]];
            answers[i] = tableDirections[pickOrder[i]];
        }

        string question = $"{string.Join(" ", tableEntries)}\n「{word}」";
        return new CipherPuzzle(question, CreateSymbolLabels(), answers);
    }

    /// <summary>
    /// B3 計算：「1+1、4−1、2×2」→ 2・3・4（ボタンは1〜4）
    /// </summary>
    /// <returns>作った謎</returns>
    private static CipherPuzzle CreateCalc()
    {
        InputDirection[] answers = new InputDirection[AnswerLength];
        string[] expressions = new string[AnswerLength];

        for (int i = 0; i < AnswerLength; i++)
        {
            int result = UnityEngine.Random.Range(MinNumber, MaxNumber + 1);
            expressions[i] = CreateExpression(result);

            // 1〜4 を InputDirection の並び（0〜3番目）に当てはめる
            answers[i] = AllDirections[result - MinNumber];
        }

        string question = string.Join("、", expressions);
        return new CipherPuzzle(question, (string[])NumberLabels.Clone(), answers);
    }

    /// <summary>
    /// B4 逆順：「☆□○ を逆から」→ ○□☆
    /// </summary>
    /// <returns>作った謎</returns>
    private static CipherPuzzle CreateReverse()
    {
        InputDirection[] shown = CreateRandomDirections();
        InputDirection[] answers = (InputDirection[])shown.Clone();
        Array.Reverse(answers);

        string question = $"「{ToSymbolText(shown)}」を逆から";
        return new CipherPuzzle(question, CreateSymbolLabels(), answers);
    }

    /// <summary>
    /// B5 数える：記号を3個・2個・1個ずつ混ぜて出し、多い順に入力する
    /// </summary>
    /// <returns>作った謎</returns>
    private static CipherPuzzle CreateCount()
    {
        // 先頭の3つを使う（先頭ほど個数が多い）
        InputDirection[] picked = CreateShuffledDirections();

        InputDirection[] answers = new InputDirection[AnswerLength];
        List<InputDirection> shown = new List<InputDirection>();

        for (int i = 0; i < AnswerLength; i++)
        {
            answers[i] = picked[i];

            for (int count = 0; count < CountOrder[i]; count++)
            {
                shown.Add(picked[i]);
            }
        }

        InputDirection[] shownArray = shown.ToArray();
        Shuffle(shownArray);

        string question = $"「{ToSymbolText(shownArray)}」を多い順に";
        return new CipherPuzzle(question, CreateSymbolLabels(), answers);
    }

    // ============================================================
    // 共通の道具
    // ============================================================

    /// <summary>
    /// 答えが result になる式を、候補の中からランダムに1つ選ぶ（B3 用）
    /// </summary>
    /// <param name="result">式の答え（1〜4）</param>
    /// <returns>例：4−1</returns>
    private static string CreateExpression(int result)
    {
        List<string> candidates = new List<string>();

        for (int a = MinNumber; a <= MaxNumber; a++)
        {
            for (int b = MinNumber; b <= MaxNumber; b++)
            {
                if (a + b == result)
                {
                    candidates.Add($"{a}+{b}");
                }

                if (a - b == result)
                {
                    candidates.Add($"{a}−{b}");
                }

                if (a * b == result)
                {
                    candidates.Add($"{a}×{b}");
                }
            }
        }

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// 記号ボタン（○△□☆）の表示を新しい配列で返す
    /// </summary>
    /// <returns>○△□☆の配列</returns>
    private static string[] CreateSymbolLabels()
    {
        return (string[])SymbolLabels.Clone();
    }

    /// <summary>
    /// 方向の並びを、記号の文字列にする
    /// </summary>
    /// <param name="directions">方向の並び</param>
    /// <returns>例：△○☆</returns>
    private static string ToSymbolText(InputDirection[] directions)
    {
        string text = string.Empty;

        for (int i = 0; i < directions.Length; i++)
        {
            text += SymbolLabels[(int)directions[i]];
        }

        return text;
    }

    /// <summary>
    /// 方向をランダムに選んで、答えの長さぶん並べる（同じ方向が重なってもよい）
    /// </summary>
    /// <returns>方向の並び</returns>
    private static InputDirection[] CreateRandomDirections()
    {
        InputDirection[] directions = new InputDirection[AnswerLength];

        for (int i = 0; i < directions.Length; i++)
        {
            directions[i] = AllDirections[UnityEngine.Random.Range(0, AllDirections.Length)];
        }

        return directions;
    }

    /// <summary>
    /// 4方向をシャッフルした新しい配列を返す
    /// </summary>
    /// <returns>4方向をランダムに並べた配列</returns>
    private static InputDirection[] CreateShuffledDirections()
    {
        InputDirection[] directions = (InputDirection[])AllDirections.Clone();
        Shuffle(directions);
        return directions;
    }

    /// <summary>
    /// 配列の中身をランダムな順に入れ替える
    /// </summary>
    /// <param name="array">入れ替える配列</param>
    /// <typeparam name="T">配列の型</typeparam>
    private static void Shuffle<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            T temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }
}