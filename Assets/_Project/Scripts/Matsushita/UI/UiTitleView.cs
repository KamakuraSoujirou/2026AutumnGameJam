using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面の表示。対戦設定の5行とゲームスタートのどれを選んでいるかを管理する。マウスのクリックも受け付ける
/// </summary>
public class UiTitleView : MonoBehaviour
{
    private const int ModeRow = 0;
    private const int CpuLevelRow = 1;
    private const int WinRow = 2;
    private const int ShortenRow = 3;
    private const int VoiceRow = 4;
    private const float DisabledAlpha = 0.35f;

    [SerializeField, Tooltip("対戦設定の行（上から：モード、CPUの強さ、勝利条件、時間短縮、読み上げ）")] private UiOptionRow[] _rows;
    [SerializeField, Tooltip("CPUの強さの行。2人対戦のときは薄くする")] private CanvasGroup _cpuLevelGroup;
    [SerializeField, Tooltip("ゲームスタートを選んでいるときに出す枠")] private GameObject _startFocusFrame;
    [SerializeField, Tooltip("ゲームスタートのボタン（マウスで押す用）")] private Button _startButton;

    private int _focusIndex;

    /// <summary>
    /// ゲームスタートがマウスでクリックされたときに呼ばれる
    /// </summary>
    public event Action StartClicked;

    /// <summary>
    /// CPU対戦を選んでいるか
    /// </summary>
    public bool IsCpuMode => _rows[ModeRow].SelectedIndex == 1;

    /// <summary>
    /// CPUの強さ（0：やさしい、1：ふつう、2：つよい）
    /// </summary>
    public int CpuLevel => _rows[CpuLevelRow].SelectedIndex;

    /// <summary>
    /// 何本先取で勝ちか
    /// </summary>
    public int WinsToMatch => _rows[WinRow].SelectedIndex == 0 ? 2 : 3;

    /// <summary>
    /// パス10回目から持ち時間を短くするか
    /// </summary>
    public bool IsShortenEnabled => _rows[ShortenRow].SelectedIndex == 0;

    /// <summary>
    /// 「あと10秒」を読み上げるか
    /// </summary>
    public bool IsVoiceEnabled => _rows[VoiceRow].SelectedIndex == 0;

    /// <summary>
    /// ゲームスタートを選んでいるか
    /// </summary>
    public bool IsStartFocused => _focusIndex == _rows.Length;

    /// <summary>
    /// マウスでクリックできるように準備する
    /// </summary>
    private void Awake()
    {
        UiEventSystem.Ensure();

        for (int i = 0; i < _rows.Length; i++)
        {
            int rowIndex = i;
            _rows[i].OptionClicked += _ => OnOptionClicked(rowIndex);
        }

        _startButton.onClick.AddListener(OnStartClicked);
    }

    /// <summary>
    /// 最初はゲームスタートを選んだ状態にする
    /// </summary>
    private void Start()
    {
        _focusIndex = _rows.Length;
        RefreshFocus();
        RefreshCpuLevelRow();
    }

    /// <summary>
    /// 選んでいる行を上下に動かす。2人対戦のときはCPUの強さを飛ばす
    /// </summary>
    /// <param name="delta">上なら -1、下なら 1</param>
    public void MoveFocus(int delta)
    {
        int next = Mathf.Clamp(_focusIndex + delta, 0, _rows.Length);

        if (next == CpuLevelRow && !IsCpuMode)
        {
            next = Mathf.Clamp(next + delta, 0, _rows.Length);
        }

        _focusIndex = next;
        RefreshFocus();
    }

    /// <summary>
    /// 選んでいる行の選択肢を左右に動かす
    /// </summary>
    /// <param name="delta">左なら -1、右なら 1</param>
    public void ChangeOption(int delta)
    {
        if (IsStartFocused)
        {
            return;
        }

        _rows[_focusIndex].Move(delta);
        RefreshCpuLevelRow();
    }

    /// <summary>
    /// クリックされた選択肢の行を、選んでいる行にする（選択肢を変える処理は行の側で済んでいる）
    /// </summary>
    /// <param name="rowIndex">クリックされた行の番号</param>
    private void OnOptionClicked(int rowIndex)
    {
        _focusIndex = rowIndex;
        RefreshFocus();
        RefreshCpuLevelRow();
        UiEventSystem.ClearSelection();
    }

    /// <summary>
    /// ゲームスタートがクリックされたので、ゲームスタートを選んだ状態にして知らせる
    /// </summary>
    private void OnStartClicked()
    {
        _focusIndex = _rows.Length;
        RefreshFocus();
        UiEventSystem.ClearSelection();
        StartClicked?.Invoke();
    }

    /// <summary>
    /// 選んでいる行にだけ枠を出す
    /// </summary>
    private void RefreshFocus()
    {
        for (int i = 0; i < _rows.Length; i++)
        {
            _rows[i].SetFocused(i == _focusIndex);
        }

        _startFocusFrame.SetActive(IsStartFocused);
    }

    /// <summary>
    /// 2人対戦のときは、CPUの強さの行を薄くして、クリックも効かなくする
    /// </summary>
    private void RefreshCpuLevelRow()
    {
        _cpuLevelGroup.alpha = IsCpuMode ? 1f : DisabledAlpha;
        _cpuLevelGroup.interactable = IsCpuMode;
    }
}
