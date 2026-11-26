using UnityEngine;
using UnityEngine.UI;

public enum WireColor
{
    Red,
    Blue,
    Yellow,
    Green
}

/// <summary>
/// 配線ギミック
/// </summary>
public class Wiringgimmick : Gimmick
{
    [SerializeField] Text _Question;
    [SerializeField] Text _serialNumberText;
    [SerializeField] Text _lampText;
    private int _serialNumber;
    // ランプの点灯状態
    private bool _isLampOn;

    private WireColor[] _currentColors;

    // 今回の問題で指定された色
    private WireColor _targetColor;

    // 問題文
    private string _questionText;

    // 正解方向
    private InputDirection _nextAnswer;

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
    /// 配線と問題を生成する
    /// </summary>
    public override void CreatePuzzle(int stage)
    {
        // シリアル番号をランダム生成（10～99）
        _serialNumber = Random.Range(10, 100);
        _serialNumberText.text = _serialNumber.ToString();

        // ランプの点灯・消灯をランダムに決定
        _isLampOn = Random.Range(0, 2) == 1;
        if (_isLampOn)
        {
            _lampText.text = "Lamp ON";
        }
        else
        {
            _lampText.text = "Lamp OFF";
        }

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

        //問題を生成
        //CreateT1Puzzle();
        //CreateT2Puzzle();
        //CreateT4Puzzle();
        CreateT5Puzzle();
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

        // 問題文
        _questionText =
            "ランプが点灯なら★の反対側、消灯なら★の線";

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
    /// 問題文を出力する
    /// </summary>
    public void QuestionTextOut()
    {
        _Question.text = _questionText;

        Debug.Log(_nextAnswer);
    }

    /// <summary>
    /// 指定された色がある方向を取得する
    /// </summary>
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
    /// ボタン入力の判定
    /// </summary>
    public override AnswerResult Answer(InputDirection direction)
    {
        // 入力方向が正解方向と一致するか
        if (direction == _nextAnswer)
        {
            Debug.Log("Correct");
            return AnswerResult.Correct;
        }

        // 正解方向と一致しなかった場合
        Debug.Log("Wrong");

        return AnswerResult.Wrong;
    }

    public void ButtonUp()
    {
        Answer(InputDirection.Up);
    }
    public void ButtonDown()
    {
        Answer(InputDirection.Down);
    }
    public void ButtonRight()
    {
        Answer(InputDirection.Right);
    }
    public void ButtonLeft()
    {
        Answer(InputDirection.Left);
    }
}