using UnityEngine;

/// <summary>
/// 対戦設定の1行（見出しと、横に並んだ選択肢）。左右で選択肢を変える
/// </summary>
public class UiOptionRow : MonoBehaviour
{
    [SerializeField, Tooltip("横に並んだ選択肢")] private UiChip[] _chips;
    [SerializeField, Tooltip("この行を選んでいるときに出す枠")] private GameObject _focusFrame;
    [SerializeField, Tooltip("最初に選んでおく選択肢の番号")] private int _selectedIndex;

    /// <summary>
    /// 選んでいる選択肢の番号（左から 0, 1, 2）
    /// </summary>
    public int SelectedIndex => _selectedIndex;

    /// <summary>
    /// 最初の見た目をそろえる
    /// </summary>
    private void Start()
    {
        RefreshChips();
    }

    /// <summary>
    /// 選択肢を左右に動かす（端で止まる）
    /// </summary>
    /// <param name="delta">左なら -1、右なら 1</param>
    public void Move(int delta)
    {
        _selectedIndex = Mathf.Clamp(_selectedIndex + delta, 0, _chips.Length - 1);
        RefreshChips();
    }

    /// <summary>
    /// この行を選んでいるかを切り替える
    /// </summary>
    /// <param name="isFocused">選んでいれば true</param>
    public void SetFocused(bool isFocused)
    {
        _focusFrame.SetActive(isFocused);
    }

    /// <summary>
    /// 選んでいる選択肢だけを強調する
    /// </summary>
    private void RefreshChips()
    {
        for (int i = 0; i < _chips.Length; i++)
        {
            _chips[i].SetSelected(i == _selectedIndex);
        }
    }
}
