using System;
using UnityEngine;

/// <summary>
/// 対戦設定の1行（見出しと、横に並んだ選択肢）。左右キーかマウスクリックで選択肢を変える
/// </summary>
public class UiOptionRow : MonoBehaviour
{
    [SerializeField, Tooltip("横に並んだ選択肢")] private UiChip[] _chips;
    [SerializeField, Tooltip("この行を選んでいるときに出す枠")] private GameObject _focusFrame;
    [SerializeField, Tooltip("最初に選んでおく選択肢の番号")] private int _selectedIndex;

    /// <summary>
    /// 選択肢がマウスでクリックされたときに呼ばれる（引数はクリックされた選択肢の番号）
    /// </summary>
    public event Action<int> OptionClicked;

    /// <summary>
    /// 選んでいる選択肢の番号（左から 0, 1, 2）
    /// </summary>
    public int SelectedIndex => _selectedIndex;

    /// <summary>
    /// 各選択肢のクリックを受け取れるようにする
    /// </summary>
    private void Awake()
    {
        for (int i = 0; i < _chips.Length; i++)
        {
            if (_chips[i].Button == null)
            {
                continue;
            }

            int index = i;
            _chips[i].Button.onClick.AddListener(() => OnChipClicked(index));
        }
    }

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
        Select(_selectedIndex + delta);
    }

    /// <summary>
    /// 選択肢を番号で選ぶ（範囲の外なら端に止める）
    /// </summary>
    /// <param name="index">選ぶ選択肢の番号（左から 0, 1, 2）</param>
    public void Select(int index)
    {
        _selectedIndex = Mathf.Clamp(index, 0, _chips.Length - 1);
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
    /// 選択肢がクリックされたので、それを選んで知らせる
    /// </summary>
    /// <param name="index">クリックされた選択肢の番号</param>
    private void OnChipClicked(int index)
    {
        Select(index);
        OptionClicked?.Invoke(index);
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
