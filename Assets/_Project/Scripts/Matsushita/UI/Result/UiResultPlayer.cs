using TMPro;
using UnityEngine;

/// <summary>
/// リザルト画面のキャラ1人分（勝ち／負けの見た目、「WIN」「LOSE」の文字、名前）
/// </summary>
public class UiResultPlayer : MonoBehaviour
{
    [SerializeField, Tooltip("勝ったときの見た目（勝ち顔）")] private GameObject _winVisual;
    [SerializeField, Tooltip("負けたときの見た目（アフロ）")] private GameObject _loseVisual;
    [SerializeField, Tooltip("勝ったときに出す「WIN」の文字")] private GameObject _winTag;
    [SerializeField, Tooltip("負けたときに出す「LOSE」の文字")] private GameObject _loseTag;
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
        _winTag.SetActive(isWinner);
        _loseTag.SetActive(!isWinner);
        _nameText.text = playerName;
    }
}
