using UnityEngine;

/// <summary>
/// 勝ち星の並び。取ったラウンドの数だけ点灯し、先取数より多い星は隠す
/// </summary>
public class UiWinStars : MonoBehaviour
{
    [SerializeField, Tooltip("星の全体（先取数に合わせて隠す）")] private GameObject[] _stars;
    [SerializeField, Tooltip("星の点灯した見た目")] private GameObject[] _litVisuals;

    /// <summary>
    /// 先取数に合わせて、星の数を決める
    /// </summary>
    /// <param name="winsToMatch">何本先取で勝ちか</param>
    public void SetWinsToMatch(int winsToMatch)
    {
        for (int i = 0; i < _stars.Length; i++)
        {
            _stars[i].SetActive(i < winsToMatch);
        }
    }

    /// <summary>
    /// 取ったラウンドの数だけ星を点灯する
    /// </summary>
    /// <param name="wins">取ったラウンド数</param>
    public void SetWins(int wins)
    {
        for (int i = 0; i < _litVisuals.Length; i++)
        {
            _litVisuals[i].SetActive(i < wins);
        }
    }
}
