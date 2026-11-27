using TMPro;
using UnityEngine;

/// <summary>
/// リザルトの表の1行（ラウンド・仕組み・取った側の★・決着・パス回数）
/// </summary>
public class UiResultRow : MonoBehaviour
{
    [SerializeField, Tooltip("ラウンドの番号")] private TMP_Text _roundText;
    [SerializeField, Tooltip("仕組み（ギミック）の名前")] private TMP_Text _gimmickText;
    [SerializeField, Tooltip("P1がラウンドを取ったときに出す★")] private GameObject _player1Star;
    [SerializeField, Tooltip("P2がラウンドを取ったときに出す★")] private GameObject _player2Star;
    [SerializeField, Tooltip("決着（爆発か解除）")] private TMP_Text _resultText;
    [SerializeField, Tooltip("パス回数")] private TMP_Text _passText;

    /// <summary>
    /// この行を表示する
    /// </summary>
    /// <param name="round">ラウンドの番号（1から）</param>
    /// <param name="result">そのラウンドの結果</param>
    public void Show(int round, UiResultRound result)
    {
        gameObject.SetActive(true);
        _roundText.text = round.ToString();
        _gimmickText.text = result.GimmickName;
        _player1Star.SetActive(result.WinnerIndex == 0);
        _player2Star.SetActive(result.WinnerIndex == 1);
        _resultText.text = result.IsDefused ? "解除" : "爆発";
        _passText.text = $"{result.PassCount:00}回";
    }

    /// <summary>
    /// この行を隠す（ラウンドが少ないとき）
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
