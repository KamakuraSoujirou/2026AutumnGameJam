using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 配線ギミックの表示。Wiringgimmick が問題を作るたびに呼ばれる
/// </summary>
public class WirePuzzleView : MonoBehaviour
{
    // 切った線の濃さ（0で見えない、1でそのまま）
    private const float CutAlpha = 0.2f;

    [Header("配線の画像")]
    [SerializeField] private Image _upWire;
    [SerializeField] private Image _downWire;
    [SerializeField] private Image _leftWire;
    [SerializeField] private Image _rightWire;

    [Header("色の頭文字表示")]
    [SerializeField] private Text _upText;
    [SerializeField] private Text _downText;
    [SerializeField] private Text _leftText;
    [SerializeField] private Text _rightText;

    /// <summary>
    /// 上下左右に配線の色を出す。切った線も元に戻す
    /// </summary>
    /// <param name="colors">配線の色（InputDirection の並び：Up, Down, Left, Right の順）</param>
    public void ShowWires(WireColor[] colors)
    {
        SetWire(_upWire, _upText, colors[0]);
        SetWire(_downWire, _downText, colors[1]);
        SetWire(_leftWire, _leftText, colors[2]);
        SetWire(_rightWire, _rightText, colors[3]);
    }

    /// <summary>
    /// 切った線を薄くする
    /// </summary>
    /// <param name="direction">切った線の方向</param>
    public void CutWire(InputDirection direction)
    {
        Image wire = GetWire(direction);

        if (wire == null)
        {
            return;
        }

        Color color = wire.color;
        color.a = CutAlpha;
        wire.color = color;
    }

    /// <summary>
    /// 方向に対応する配線の画像を返す
    /// </summary>
    /// <param name="direction">方向</param>
    /// <returns>その方向の配線の画像</returns>
    private Image GetWire(InputDirection direction)
    {
        switch (direction)
        {
            case InputDirection.Up:
                return _upWire;
            case InputDirection.Down:
                return _downWire;
            case InputDirection.Left:
                return _leftWire;
            case InputDirection.Right:
                return _rightWire;
            default:
                return null;
        }
    }

    /// <summary>
    /// 配線の色と文字を設定する
    /// </summary>
    /// <param name="wire">配線の画像</param>
    /// <param name="text">頭文字を出すテキスト</param>
    /// <param name="color">配線の色</param>
    private void SetWire(
        Image wire,
        Text text,
        WireColor color)
    {
        if (wire != null)
        {
            wire.color = GetUnityColor(color);
        }

        if (text != null)
        {
            text.text = GetInitial(color);
        }
    }

    /// <summary>
    /// WireColorをUnityのColorに変換する
    /// </summary>
    /// <param name="color">配線の色</param>
    /// <returns>Unity の色</returns>
    private Color GetUnityColor(WireColor color)
    {
        switch (color)
        {
            case WireColor.Red:
                return Color.red;

            case WireColor.Blue:
                return Color.blue;

            case WireColor.Yellow:
                return Color.yellow;

            case WireColor.Green:
                return Color.green;

            default:
                return Color.white;
        }
    }

    /// <summary>
    /// 色の頭文字を取得する
    /// </summary>
    /// <param name="color">配線の色</param>
    /// <returns>R・B・Y・G のどれか</returns>
    private string GetInitial(WireColor color)
    {
        switch (color)
        {
            case WireColor.Red:
                return "R";

            case WireColor.Blue:
                return "B";

            case WireColor.Yellow:
                return "Y";

            case WireColor.Green:
                return "G";

            default:
                return "?";
        }
    }
}
