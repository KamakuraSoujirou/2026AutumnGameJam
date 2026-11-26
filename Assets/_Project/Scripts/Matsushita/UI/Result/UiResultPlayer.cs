using TMPro;
using UnityEngine;

/// <summary>
/// リザルト画面のキャラ1人分（勝ち／負けの見た目、「勝ち」「負け」の札、名前）
/// </summary>
public class UiResultPlayer : MonoBehaviour
{
    private static readonly Color WinColor = new Color(0.92f, 0.54f, 0f);
    private static readonly Color LoseColor = new Color(0.45f, 0.45f, 0.45f);

    [SerializeField, Tooltip("勝ったときの見た目（勝ち顔）")] private GameObject _winVisual;
    [SerializeField, Tooltip("負けたときの見た目（アフロ）")] private GameObject _loseVisual;
    [SerializeField, Tooltip("「勝ち」「負け」の札")] private TMP_Text _tagText;
    [SerializeField, Tooltip("プレイヤー名")] private TMP_Text _nameText;

    /// <summary>
    /// 勝ち負けと名前を表示に反映する
    /// </summary>
    /// <param name="isWinner">勝った側なら true</param>
    /// <param name="playerName">プレイヤー名</param>
    public void Show(bool isWinner, string playerName)
    {
        _winVisual.SetActive(isWinner);
        _loseVisual.SetActive(!isWinner);
        _tagText.text = isWinner ? "勝ち" : "負け";
        _tagText.color = isWinner ? WinColor : LoseColor;
        _nameText.text = playerName;
    }
}
