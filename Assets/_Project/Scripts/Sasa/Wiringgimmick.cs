using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 配線ギミック。上下左右の4本から、問題に合う線を1本切る
/// </summary>
public class Wiringgimmick : Gimmick
{
    // 同じ種類を続けて出してよい回数（3回続けない → 2回まで）
    private const int MaxSameTypeInRow = 2;

    // 線の本数（上下左右）
    private const int WireCount = 4;

    // T3 連想の、JSON での種類名
    private const string AssociationType = "T3";

    // T7 仲間外れの記号版で使う記号
    private static readonly string[] OddOneOutSymbols = { "○", "△", "□", "☆" };

    // T7 仲間外れの数字版で使う数字（1桁にして、すぐ読めるようにする）
    private static readonly int[] EvenNumbers = { 2, 4, 6, 8 };
    private static readonly int[] OddNumbers = { 1, 3, 5, 7, 9 };

    [SerializeField] private WirePuzzleView _view;

    // 試合画面の爆弾（試合では、線の色・シリアル番号・ランプをここに出す。テストシーンでは空でよい）
    [SerializeField] private UiBombView _bombView;

    [SerializeField, FormerlySerializedAs("_Question")] private Text _questionLabel;
    [SerializeField] private Text _serialNumberText;
    [SerializeField] private Text _lampText;

    // 難易度ごとに出す謎の種類（Notion「難易度の段階」の表のとおり）
    [SerializeField] private WirePuzzleType[] _stage1Types = { WirePuzzleType.ColorMark };
    [SerializeField] private WirePuzzleType[] _stage2Types = { WirePuzzleType.ColorMark, WirePuzzleType.ColorWord, WirePuzzleType.OddOneOut };
    [SerializeField] private WirePuzzleType[] _stage3Types = { WirePuzzleType.ColorWord, WirePuzzleType.Association, WirePuzzleType.Serial, WirePuzzleType.Memory };
    [SerializeField] private WirePuzzleType[] _stage4Types = { WirePuzzleType.Association, WirePuzzleType.Serial, WirePuzzleType.Lamp, WirePuzzleType.Reverse, WirePuzzleType.OddOneOut, WirePuzzleType.Memory };

    private int _serialNumber;
    // ランプの点灯状態
    private bool _isLampOn;

    private WireColor[] _currentColors;

    // 線ごとの記号や数字（InputDirection の並び。T7 以外は空）
    private readonly string[] _symbols = new string[WireCount];

    // 今回の問題で指定された色
    private WireColor _targetColor;

    // 問題文
    private string _questionText = string.Empty;

    // 正解方向
    private InputDirection _nextAnswer;

    private WirePuzzleType _lastType;
    private int _sameTypeCount;

    // T8 記憶：このラウンドで直前に正解で切られた線（相手が切った線）
    private bool _hasLastCut;
    private InputDirection _lastCutDirection;
    private WireColor _lastCutColor;

    // T3 連想の問題（初めて使うときに読む）
    private PuzzleDatabase _associationDatabase;

    /// <summary>
    /// 現在の配線色
    /// </summary>
    public WireColor[] CurrentColors => _currentColors;

    /// <summary>
    /// 液晶に表示する問題文
    /// </summary>
    public override string QuestionText => _questionText;

    /// <summary>
    /// 次に押すと正解の方向
    /// </summary>
    public override InputDirection NextAnswer => _nextAnswer;

    /// <summary>
    /// T3 連想の問題（Resources/Puzzles/WireAssociation.json）。初めて使うときに読む
    /// </summary>
    private PuzzleDatabase AssociationDatabase
    {
        get
        {
            if (_associationDatabase == null)
            {
                _associationDatabase = new PuzzleDatabase(PuzzleDatabase.WireAssociationPath);
            }

            return _associationDatabase;
        }
    }

    /// <summary>
    /// ラウンドの最初に呼ばれる。同じ種類の連続回数を数え直し、切った線の記憶（T8）も消す
    /// </summary>
    public override void ResetRound()
    {
        _sameTypeCount = 0;
        _hasLastCut = false;
    }

    /// <summary>
    /// 配線と問題を生成する
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    public override void CreatePuzzle(int stage)
    {
        // シリアル番号をランダム生成（10～99）
        _serialNumber = Random.Range(10, 100);

        // ランプの点灯・消灯をランダムに決定
        _isLampOn = Random.Range(0, 2) == 1;

        // 4色を用意
        WireColor[] colors =
        {
            WireColor.Red,
            WireColor.Blue,
            WireColor.Yellow,
            WireColor.Green
        };

        // シャッフル
        for (int i = colors.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);

            (colors[i], colors[j]) = (colors[j], colors[i]);
        }

        // 前回と同じ配置になった場合は2色を交換する
        if (_currentColors != null &&
            IsSameArrangement(colors, _currentColors))
        {
            (colors[0], colors[1]) = (colors[1], colors[0]);
        }

        // 現在の配線色として保存
        _currentColors = colors;

        // 記号は T7 のときだけ出すので、いったん全部消しておく
        for (int i = 0; i < WireCount; i++)
        {
            _symbols[i] = string.Empty;
        }

        // 難易度に合った種類の問題を生成
        WirePuzzleType type = PickPuzzleType(stage);

        switch (type)
        {
            case WirePuzzleType.ColorMark:
                CreateT1Puzzle();
                break;
            case WirePuzzleType.ColorWord:
                CreateT2Puzzle();
                break;
            case WirePuzzleType.Serial:
                CreateT4Puzzle();
                break;
            case WirePuzzleType.Lamp:
                CreateT5Puzzle();
                break;
            case WirePuzzleType.Association:
                CreateT3Puzzle();
                break;
            case WirePuzzleType.Reverse:
                CreateT6Puzzle();
                break;
            case WirePuzzleType.OddOneOut:
                CreateT7Puzzle();
                break;
            case WirePuzzleType.Memory:
                CreateT8Puzzle();
                break;
        }

        ShowPuzzle();
        Debug.Log($"[配線] 段階{stage}（{type}）：{_questionText}（記号：{string.Join(" ", _symbols)}）（正解：{_nextAnswer}）");
    }

    /// <summary>
    /// 段階に合う謎の種類を均等に選ぶ。同じ種類が3回続かないようにする。
    /// T8 記憶は、このラウンドでまだ線が切られていない（ラウンド最初のパス）ときは出さない
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    /// <returns>選んだ謎の種類</returns>
    private WirePuzzleType PickPuzzleType(int stage)
    {
        List<WirePuzzleType> candidates = new List<WirePuzzleType>(GetStageTypes(stage));

        if (!_hasLastCut)
        {
            candidates.Remove(WirePuzzleType.Memory);
        }

        // T3 連想は、JSON に問題が1問もなければ出さない
        if (candidates.Contains(WirePuzzleType.Association) && AssociationDatabase.GetPuzzles(AssociationType).Count == 0)
        {
            candidates.Remove(WirePuzzleType.Association);
        }

        // 出せる種類がなくなったときは、T1 を出す
        if (candidates.Count == 0)
        {
            candidates.Add(WirePuzzleType.ColorMark);
        }

        // 2回続いていたら外す（候補が1つしかない段階は外せないのでそのまま）
        if (_sameTypeCount >= MaxSameTypeInRow && candidates.Count > 1)
        {
            candidates.Remove(_lastType);
        }

        WirePuzzleType picked = candidates[Random.Range(0, candidates.Count)];

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
    private WirePuzzleType[] GetStageTypes(int stage)
    {
        WirePuzzleType[] types;

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
            Debug.LogWarning($"[配線] 段階{stage}の設定がないので、T1（色指定）を出します");
            return new[] { WirePuzzleType.ColorMark };
        }

        return types;
    }

    /// <summary>
    /// T1 色指定問題を生成する
    /// </summary>
    private void CreateT1Puzzle()
    {
        // ★の色をランダムに選ぶ
        _targetColor = (WireColor)Random.Range(0, 4);

        // ★だけ色を変更した問題文を作成
        _questionText =
            $"<color={GetColorCode(_targetColor)}>★</color>と同じ色の線を切れ";

        // ★と同じ色の配線がある方向を正解にする
        _nextAnswer = GetDirectionFromColor(_targetColor);
    }

    /// <summary>
    /// T2 文字と色の食い違い問題を生成
    /// </summary>
    private void CreateT2Puzzle()
    {
        // 文字の意味（正解色）をランダムに選ぶ
        _targetColor = (WireColor)Random.Range(0, 4);

        // 文字の表示色をランダムに選ぶ
        WireColor displayColor;

        do
        {
            displayColor = (WireColor)Random.Range(0, 4);
        }
        while (displayColor == _targetColor);

        // 文字の意味は正解色、文字自体の色は別の色にする
        _questionText =
            $"<color={GetColorCode(displayColor)}>{GetColorName(_targetColor)}</color>";

        // 文字の意味に対応する色の配線を正解にする
        _nextAnswer = GetDirectionFromColor(_targetColor);
    }

    /// <summary>
    /// T4 シリアル番号条件問題を生成
    /// </summary>
    private void CreateT4Puzzle()
    {
        // 質問タイプをランダム選択（0～2）
        int questionType = Random.Range(0, 3);

        // 条件の判定結果
        bool result = false;

        switch (questionType)
        {
            case 0:
                // 偶奇判定
                result = _serialNumber % 2 == 0;
                _questionText = "シリアルが偶数なら赤、奇数なら青";
                break;

            case 1:
                // 50以上判定
                result = _serialNumber >= 50;
                _questionText = "シリアルが50以上なら赤、未満なら青";
                break;

            case 2:
                // 各桁の和判定
                int digitSum =
                    _serialNumber / 10 +
                    _serialNumber % 10;

                result = digitSum >= 10;
                _questionText = "各桁の和が10以上なら赤、未満なら青";
                break;
        }

        // Trueなら赤、Falseなら青
        WireColor targetColor = result
            ? WireColor.Red
            : WireColor.Blue;

        // 正解色が配置されている方向を取得
        _nextAnswer = GetDirectionFromColor(targetColor);
    }

    /// <summary>
    /// T5 ランプ条件問題を生成
    /// </summary>
    private void CreateT5Puzzle()
    {
        // ★の色をランダムに決定
        _targetColor = (WireColor)Random.Range(0, 4);

        // ★の色が配置されている方向を取得
        InputDirection starDirection =
            GetDirectionFromColor(_targetColor);

        // 問題文（★の色が分からないと解けないので、★に色を付ける）
        _questionText =
            $"ランプが点灯なら<color={GetColorCode(_targetColor)}>★</color>の反対側、消灯なら<color={GetColorCode(_targetColor)}>★</color>の線";

        // ランプの状態によって正解方向を決定
        if (_isLampOn)
        {
            // 点灯：★の反対方向
            _nextAnswer = GetOppositeDirection(starDirection);
        }
        else
        {
            // 消灯：★と同じ方向
            _nextAnswer = starDirection;
        }
    }

    /// <summary>
    /// T3 連想問題を生成する。JSON の問題文をそのまま出し、answer の色の線を正解にする
    /// </summary>
    private void CreateT3Puzzle()
    {
        PuzzleData puzzle = AssociationDatabase.Draw(AssociationType);

        if (puzzle == null || !TryGetColorFromName(puzzle.Answer, out WireColor color))
        {
            Debug.LogWarning($"[配線] T3 連想の答え「{puzzle?.Answer}」が色名（赤・青・黄・緑）ではないので、T1 を出します");
            CreateT1Puzzle();
            return;
        }

        _targetColor = color;
        _questionText = puzzle.Text;
        _nextAnswer = GetDirectionFromColor(color);
    }

    /// <summary>
    /// JSON に書いた色名を配線の色に変える（赤・青・黄・緑。ひらがなと「黄色」も読める）
    /// </summary>
    /// <param name="colorName">色名</param>
    /// <param name="color">配線の色</param>
        switch (colorName == null ? string.Empty : colorName.Trim())
    private static bool TryGetColorFromName(string colorName, out WireColor color)
    {
        switch (colorName == null ? string.Empty : colorName.Trim())
        {
            case "赤":
            case "あか":
                color = WireColor.Red;
                return true;
            case "青":
            case "あお":
                color = WireColor.Blue;
                return true;
            case "黄":
            case "黄色":
            case "きいろ":
                color = WireColor.Yellow;
                return true;
            case "緑":
            case "みどり":
                color = WireColor.Green;
                return true;
            default:
                color = WireColor.Red;
                return false;
        }
    }

    /// <summary>
    /// T6 逆さ読み問題を生成する。「みどりをきれ」を後ろから読んだ「れきをりどみ」を出す
    /// </summary>
    private void CreateT6Puzzle()
    {
        _targetColor = (WireColor)Random.Range(0, 4);

        char[] letters = $"{GetColorName(_targetColor)}をきれ".ToCharArray();
        System.Array.Reverse(letters);

        _questionText = new string(letters);
        _nextAnswer = GetDirectionFromColor(_targetColor);
    }

    /// <summary>
    /// T7 仲間外れ問題を生成する。4本の線に記号か数字を出し、1本だけ違う線を正解にする
    /// </summary>
    private void CreateT7Puzzle()
    {
        // 仲間外れにする線
        int oddIndex = Random.Range(0, WireCount);

        // 記号版と数字版を半分ずつ
        if (Random.Range(0, 2) == 0)
        {
            SetOddOneOutSymbols(oddIndex);
        }
        else
        {
            SetOddOneOutNumbers(oddIndex);
        }

        _questionText = "仲間外れの線を切れ";
        _nextAnswer = (InputDirection)oddIndex;
    }

    /// <summary>
    /// T7 記号版：3本に同じ記号、1本だけ別の記号を付ける（○○△○）
    /// </summary>
    /// <param name="oddIndex">仲間外れにする線の番号</param>
    private void SetOddOneOutSymbols(int oddIndex)
    {
        int sameIndex = Random.Range(0, OddOneOutSymbols.Length);

        // 同じ記号以外から1つ選ぶ
        int differentIndex = (sameIndex + Random.Range(1, OddOneOutSymbols.Length)) % OddOneOutSymbols.Length;

        for (int i = 0; i < WireCount; i++)
        {
            _symbols[i] = i == oddIndex ? OddOneOutSymbols[differentIndex] : OddOneOutSymbols[sameIndex];
        }
    }

    /// <summary>
    /// T7 数字版：3本に偶数、1本だけ奇数を付ける（半分は逆に、奇数3つと偶数1つ）。数字はすべて違うものにする
    /// </summary>
    /// <param name="oddIndex">仲間外れにする線の番号</param>
    private void SetOddOneOutNumbers(int oddIndex)
    {
        bool isMajorityEven = Random.Range(0, 2) == 0;
        int[] majority = PickDistinctNumbers(isMajorityEven ? EvenNumbers : OddNumbers, WireCount - 1);
        int[] minority = isMajorityEven ? OddNumbers : EvenNumbers;
        int majorityIndex = 0;

        for (int i = 0; i < WireCount; i++)
        {
            if (i == oddIndex)
            {
                _symbols[i] = minority[Random.Range(0, minority.Length)].ToString();
            }
            else
            {
                _symbols[i] = majority[majorityIndex].ToString();
                majorityIndex++;
            }
        }
    }

    /// <summary>
    /// T8 記憶問題を生成する。相手がさっき切った線と同じ位置か、同じ色の線を正解にする
    /// （線の並びはパスごとに変わるので、位置と色で答えが変わる）
    /// </summary>
    private void CreateT8Puzzle()
    {
        if (Random.Range(0, 2) == 0)
        {
            // 液晶の1行に収まる長さにする
            _questionText = "さっき相手が切った位置を切れ";
            _nextAnswer = _lastCutDirection;
        }
        else
        {
            _questionText = "さっき相手が切った色を切れ";
            _nextAnswer = GetDirectionFromColor(_lastCutColor);
        }
    }

    /// <summary>
    /// 数字の候補から、重ならないように指定の数だけ選ぶ
    /// </summary>
    /// <param name="source">数字の候補</param>
    /// <param name="count">選ぶ数（候補の数以下）</param>
    /// <returns>選んだ数字</returns>
    private int[] PickDistinctNumbers(int[] source, int count)
    {
        int[] shuffled = (int[])source.Clone();

        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        int[] picked = new int[count];
        System.Array.Copy(shuffled, picked, count);
        return picked;
    }

    /// <summary>
    /// 指定方向の反対方向を取得
    /// </summary>
    /// <param name="direction">元の方向</param>
    /// <returns>反対の方向</returns>
    private InputDirection GetOppositeDirection(
        InputDirection direction)
    {
        switch (direction)
        {
            case InputDirection.Up:
                return InputDirection.Down;

            case InputDirection.Down:
                return InputDirection.Up;

            case InputDirection.Left:
                return InputDirection.Right;

            case InputDirection.Right:
                return InputDirection.Left;

            default:
                return InputDirection.Up;
        }
    }

    /// <summary>
    /// WireColorを色の名前に変換
    /// </summary>
    /// <param name="color">色</param>
    /// <returns>ひらがなの色の名前</returns>
    private string GetColorName(WireColor color)
    {
        switch (color)
        {
            case WireColor.Red:
                return "あか";

            case WireColor.Blue:
                return "あお";

            case WireColor.Yellow:
                return "きいろ";

            case WireColor.Green:
                return "みどり";

            default:
                return "不明";
        }
    }

    /// <summary>
    /// 配線・記号・シリアル番号・ランプ・問題文を画面に出す（置いていないものは飛ばす）
    /// </summary>
    private void ShowPuzzle()
    {
        if (_view != null)
        {
            _view.ShowWires(_currentColors, _symbols);
        }

        if (_bombView != null)
        {
            for (int i = 0; i < _currentColors.Length; i++)
            {
                InputDirection direction = (InputDirection)i;
                _bombView.SetWire(direction, ToUiWireColor(_currentColors[i]), UiWireState.Normal);

                // 記号は隅だと小さくて○と□を見分けにくいので、ボタンの真ん中に大きく出す（空なら色名の頭文字に戻る）
                _bombView.SetButtonLabel(direction, _symbols[i]);
            }

            _bombView.SetSerial(_serialNumber);
            _bombView.SetLamp(_isLampOn);
        }

        if (_serialNumberText != null)
        {
            _serialNumberText.text = _serialNumber.ToString();
        }

        if (_lampText != null)
        {
            _lampText.text = _isLampOn ? "Lamp ON" : "Lamp OFF";
        }

        QuestionTextOut();
    }

    /// <summary>
    /// 問題文を出力する（テストシーン用。試合では液晶に爆弾本体が出す）
    /// </summary>
    public void QuestionTextOut()
    {
        if (_questionLabel != null)
        {
            _questionLabel.text = _questionText;
        }
    }

    /// <summary>
    /// 指定された色がある方向を取得する
    /// </summary>
    /// <param name="targetColor">探す色</param>
    /// <returns>その色の線がある方向</returns>
    private InputDirection GetDirectionFromColor(WireColor targetColor)
    {
        for (int i = 0; i < _currentColors.Length; i++)
        {
            if (_currentColors[i] == targetColor)
            {
                return (InputDirection)i;
            }
        }

        // 通常ここには到達しない
        return InputDirection.Up;
    }

    /// <summary>
    /// WireColorをHTMLカラーコードに変換
    /// </summary>
    /// <param name="color">色</param>
    /// <returns>#RRGGBB の形のカラーコード</returns>
    private string GetColorCode(WireColor color)
    {
        switch (color)
        {
            case WireColor.Red:
                return "#FF0000";

            case WireColor.Blue:
                return "#0000FF";

            case WireColor.Yellow:
                return "#FFFF00";

            case WireColor.Green:
                return "#00FF00";

            default:
                return "#FFFFFF";
        }
    }

    /// <summary>
    /// 2つの配列の配置が同じか確認する
    /// </summary>
    /// <param name="a">比べる配列</param>
    /// <param name="b">比べる配列</param>
    /// <returns>同じ並びなら true</returns>
    private bool IsSameArrangement(
        WireColor[] a,
        WireColor[] b)
    {
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// ボタン入力の判定。線は1本だけ切るので、合っていれば Correct、違えば Wrong
    /// </summary>
    /// <param name="direction">切った線の方向</param>
    /// <returns>入力した結果</returns>
    public override AnswerResult Answer(InputDirection direction)
    {
        // 入力方向が正解方向と一致するか
        if (direction == _nextAnswer)
        {
            if (_view != null)
            {
                _view.CutWire(direction);
            }

            ShowWireState(direction, UiWireState.Cut);

            // 次の人の T8（記憶）のために、切った線の位置と色を覚えておく
            if (_currentColors != null)
            {
                _hasLastCut = true;
                _lastCutDirection = direction;
                _lastCutColor = _currentColors[(int)direction];
            }

            return AnswerResult.Correct;
        }

        // 正解方向と一致しなかった場合は、切った線を消す（時間減少とスタンは爆弾本体が行う）
        ShowWireState(direction, UiWireState.Gone);
        return AnswerResult.Wrong;
    }

    /// <summary>
    /// 試合画面の爆弾の線1本を、切れた・消えた見た目にする
    /// </summary>
    /// <param name="direction">切った線の方向</param>
    /// <param name="state">線の状態</param>
    private void ShowWireState(InputDirection direction, UiWireState state)
    {
        if (_bombView == null || _currentColors == null)
        {
            return;
        }

        _bombView.SetWire(direction, ToUiWireColor(_currentColors[(int)direction]), state);
    }

    /// <summary>
    /// 配線の色を、試合画面の爆弾の色に変える
    /// </summary>
    /// <param name="color">配線の色</param>
    /// <returns>試合画面の爆弾の色</returns>
    private UiWireColor ToUiWireColor(WireColor color)
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
    /// テストシーンのボタン用：上の線を切る
    /// </summary>
    public void ButtonUp()
    {
        LogAnswer(InputDirection.Up);
    }

    /// <summary>
    /// テストシーンのボタン用：下の線を切る
    /// </summary>
    public void ButtonDown()
    {
        LogAnswer(InputDirection.Down);
    }

    /// <summary>
    /// テストシーンのボタン用：右の線を切る
    /// </summary>
    public void ButtonRight()
    {
        LogAnswer(InputDirection.Right);
    }

    /// <summary>
    /// テストシーンのボタン用：左の線を切る
    /// </summary>
    public void ButtonLeft()
    {
        LogAnswer(InputDirection.Left);
    }

    /// <summary>
    /// テストシーンのボタン用：入力して結果をコンソールに出す
    /// </summary>
    /// <param name="direction">切った線の方向</param>
    private void LogAnswer(InputDirection direction)
    {
        Debug.Log($"[配線] {direction} → {Answer(direction)}");
    }
}
