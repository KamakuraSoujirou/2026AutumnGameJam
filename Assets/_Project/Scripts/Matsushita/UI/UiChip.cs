using TMPro;
using UnityEngine;

/// <summary>
/// 選択肢のボタン1つ（「2人対戦」など）。選ばれているかで見た目を切り替える
/// </summary>
public class UiChip : MonoBehaviour
{
    [SerializeField, Tooltip("選ばれているときの見た目")] private GameObject _onVisual;
    [SerializeField, Tooltip("選ばれていないときの見た目")] private GameObject _offVisual;
    [SerializeField, Tooltip("ボタンの文字")] private TMP_Text _label;

    /// <summary>
    /// ボタンの文字
    /// </summary>
    public TMP_Text Label => _label;

    /// <summary>
    /// 選ばれているかを切り替える
    /// </summary>
    /// <param name="isSelected">選ばれていれば true</param>
    public void SetSelected(bool isSelected)
    {
        _onVisual.SetActive(isSelected);
        _offVisual.SetActive(!isSelected);
    }
}
