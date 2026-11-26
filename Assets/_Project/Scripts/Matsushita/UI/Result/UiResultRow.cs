using TMPro;
using UnityEngine;

/// <summary>
/// リザルトの表の1行（「Round 1」と、そのラウンドを取った側の★）
/// </summary>
public class UiResultRow : MonoBehaviour
{
    [SerializeField, Tooltip("ラウンドの表示（Round 1 など）")] private TMP_Text _label;
    [SerializeField, Tooltip("P1がラウンドを取ったときに出す★（左側）")] private GameObject _player1Star;
    [SerializeField, Tooltip("P2がラウンドを取ったときに出す★（右側）")] private GameObject _player2Star;

    /// <summary>
    /// この行を表示する
    /// </summary>
    /// <param name="round">ラウンドの番号（1から）</param>
    /// <param name="winnerIndex">ラウンドを取った側（0：P1、1：P2）</param>
    public void Show(int round, int winnerIndex)
    {
        gameObject.SetActive(true);
        _label.text = $"Round {round}";
        _player1Star.SetActive(winnerIndex == 0);
        _player2Star.SetActive(winnerIndex == 1);
    }

    /// <summary>
    /// この行を隠す（ラウンドが少ないとき）
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
