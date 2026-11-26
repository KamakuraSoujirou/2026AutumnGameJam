using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 配線ギミックの表示
/// </summary>
public class WirePuzzleView : MonoBehaviour
{
    [Header("配線のMeshRenderer")]
    [SerializeField] Image _upWire;
    [SerializeField] Image _downWire;
    [SerializeField] Image _leftWire;
    [SerializeField] Image _rightWire;

    [Header("色の頭文字表示")]
    [SerializeField] Text _upText;
    [SerializeField] Text _downText;
    [SerializeField] Text _leftText;
    [SerializeField] Text _rightText;

    [Header("配線ギミック")]
    [SerializeField] Wiringgimmick _logic;

    /// <summary>
    /// 配線の色をランダム配置する
    /// </summary>
    public void RandomizeWires()
    {
        // Logic側で配線の色を生成
        _logic.CreatePuzzle(1);

        // 現在の配線色を取得
        WireColor[] colors = _logic.CurrentColors;

        // 上下左右に色を設定
        SetWire(_upWire, _upText, colors[0]);
        SetWire(_downWire, _downText, colors[1]);
        SetWire(_leftWire, _leftText, colors[2]);
        SetWire(_rightWire, _rightText, colors[3]);
    }
    

    /// <summary>
    /// 配線の色と文字を設定する
    /// </summary>
    private void SetWire(
        Image wire,
        Text text,
        WireColor color)
    {
        wire.color = GetUnityColor(color);
        text.text = GetInitial(color);
    }

    /// <summary>
    /// WireColorをUnityのColorに変換する
    /// </summary>
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