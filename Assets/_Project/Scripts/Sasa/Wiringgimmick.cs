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

    [SerializeField] private WirePuzzleView _view;
    [SerializeField, FormerlySerializedAs("_Question")] private Text _questionLabel;
    [SerializeField] private Text _serialNumberText;
    [SerializeField] private Text _lampText;

    // 難易度ごとに出す謎の種類（Notion の表から、できているものだけ）
    // 段2の T7、段3の T3・T8、段4の T3・T6・T7・T8 はまだないので入れていない
    [SerializeField] private WirePuzzleType[] _stage1Types = { WirePuzzleType.ColorMark };
    [SerializeField] private WirePuzzleType[] _stage2Types = { WirePuzzleType.ColorMark, WirePuzzleType.ColorWord };
    [SerializeField] private WirePuzzleType[] _stage3Types = { WirePuzzleType.ColorWord, WirePuzzleType.Serial };
    [SerializeField] private WirePuzzleType[] _stage4Types = { WirePuzzleType.Serial, WirePuzzleType.Lamp };

    private int _serialNumber;
    // ランプの点灯状態
    private bool _isLampOn;

    private WireColor[] _currentColors;

    // 今回の問題で指定された色
    private WireColor _targetColor;

    // 問題文
    private string _questionText = string.Empty;

    // 正解方向
    private InputDirection _nextAnswer;

    private WirePuzzleType _lastType;
    private int _sameTypeCount;

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
    /// ラウンドの最初に呼ばれる。同じ種類の連続回数を数え直す
    /// </summary>
    public override void ResetRound()
    {
        _sameTypeCount = 0;
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
        }

        ShowPuzzle();
        Debug.Log($"[配線] 段階{stage}（{type}）：{_questionText}（正解：{_nextAnswer}）");
    }

    /// <summary>
    /// 段階に合う謎の種類を均等に選ぶ。同じ種類が3回続かないようにする
    /// </summary>
    /// <param name="stage">難易度の段階（1〜4）</param>
    /// <returns>選んだ謎の種類</returns>
    private WirePuzzleType PickPuzzleType(int stage)
    {
        List<WirePuzzleType> candidates = new List<WirePuzzleType>(GetStageTypes(stage));

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
    /// 配線・シリアル番号・ランプ・問題文を画面に出す（置いていないものは飛ばす）
    /// </summary>
    private void ShowPuzzle()
    {
        if (_view != null)
        {
            _view.ShowWires(_currentColors);
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

            return AnswerResult.Correct;
        }

        // 正解方向と一致しなかった場合は、線を切らずに戻す（時間減少とスタンは爆弾本体が行う）
        return AnswerResult.Wrong;
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
