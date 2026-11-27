using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クイックテキストの入力。待機中の側が方向キーを押し続けると円形の候補を出し、離したら選んでいた文を送る。
/// 候補が出ている間（または押している間）に解除キーを押すと、何も送らずに取り消す
/// </summary>
/// <remarks>
/// 使えるかどうか（待機中か、解除中でないか）は CanUse で決める。何も設定しなければ UiGameView.CanUseQuickText を使う。
/// 持っている側の方向キーは爆弾の入力なので、ここでは見ない
/// </remarks>
public class QuickTextInput : MonoBehaviour
{
    private const int PlayerCount = 2;

    [SerializeField, Tooltip("文と候補を出す試合画面。空なら同じオブジェクトから探す")] private UiGameView _view;
    [SerializeField, Tooltip("キー入力。空なら同じオブジェクト、なければシーンから探す")] private PlayerInputReader _reader;
    [SerializeField, Min(0f), Tooltip("方向キーを押し続けてから、候補が出るまでの時間（秒）")] private float _wheelDelaySeconds = 0.2f;

    private readonly List<InputDirection>[] _heldDirections = new List<InputDirection>[PlayerCount];
    private readonly float[] _heldSeconds = new float[PlayerCount];
    private readonly bool[] _isWheelOpen = new bool[PlayerCount];
    private readonly bool[] _isCancelled = new bool[PlayerCount];
    private Func<int, bool> _canUse;

    /// <summary>
    /// クイックテキストを使えるかを返す処理（プレイヤー番号 → 使えるか）。設定しなければ、待機中で解除中でないときだけ使える
    /// </summary>
    public Func<int, bool> CanUse
    {
        set => _canUse = value;
    }

    /// <summary>
    /// 押しているキーの記録を用意して、見つからない参照を同じオブジェクトから探す
    /// </summary>
    private void Awake()
    {
        for (int i = 0; i < PlayerCount; i++)
        {
            _heldDirections[i] = new List<InputDirection>();
        }

        if (_view == null)
        {
            _view = GetComponent<UiGameView>();
        }

        if (_reader == null)
        {
            _reader = GetComponent<PlayerInputReader>();
        }

        if (_reader == null)
        {
            _reader = FindAnyObjectByType<PlayerInputReader>();
        }
    }

    /// <summary>
    /// キーの押し・離しを受け取れるようにする
    /// </summary>
    private void OnEnable()
    {
        if (_reader == null)
        {
            return;
        }

        _reader.DirectionPressed += OnDirectionPressed;
        _reader.DirectionReleased += OnDirectionReleased;
        _reader.DefusePressed += OnDefusePressed;
    }

    /// <summary>
    /// 受け取りをやめて、出ている候補を閉じる
    /// </summary>
    private void OnDisable()
    {
        if (_reader != null)
        {
            _reader.DirectionPressed -= OnDirectionPressed;
            _reader.DirectionReleased -= OnDirectionReleased;
            _reader.DefusePressed -= OnDefusePressed;
        }

        for (int i = 0; i < PlayerCount; i++)
        {
            ClearPlayer(i);
        }
    }

    /// <summary>
    /// 押し続けた時間を数えて、候補を出す。使えなくなった（爆弾を受け取った、解除が始まったなど）ときは、何も送らずにやめる
    /// </summary>
    private void Update()
    {
        for (int i = 0; i < PlayerCount; i++)
        {
            if (_heldDirections[i].Count == 0)
            {
                continue;
            }

            if (!IsUsable(i))
            {
                ClearPlayer(i);
                continue;
            }

            if (_isWheelOpen[i] || _isCancelled[i])
            {
                continue;
            }

            _heldSeconds[i] += Time.unscaledDeltaTime;

            if (_heldSeconds[i] >= _wheelDelaySeconds)
            {
                _isWheelOpen[i] = true;
                _view.SetQuickTextWheel(i, true, SelectedDirection(i));
            }
        }
    }

    /// <summary>
    /// 方向キーが押された。使える側なら、押している方向に加えて、選ぶ方向を変える
    /// </summary>
    /// <param name="player">押したプレイヤー</param>
    /// <param name="direction">押した方向</param>
    private void OnDirectionPressed(int player, InputDirection direction)
    {
        if (!IsUsable(player) || _heldDirections[player].Contains(direction))
        {
            return;
        }

        _heldDirections[player].Add(direction);

        if (_isWheelOpen[player])
        {
            _view.SetQuickTextWheel(player, true, direction);
        }
    }

    /// <summary>
    /// 方向キーが離された。全部離したら、取り消していなければ選んでいた文を送る
    /// </summary>
    /// <param name="player">離したプレイヤー</param>
    /// <param name="direction">離した方向</param>
    private void OnDirectionReleased(int player, InputDirection direction)
    {
        List<InputDirection> held = _heldDirections[player];

        if (!held.Remove(direction))
        {
            return;
        }

        if (held.Count > 0)
        {
            if (_isWheelOpen[player])
            {
                _view.SetQuickTextWheel(player, true, SelectedDirection(player));
            }

            return;
        }

        bool isSend = !_isCancelled[player] && IsUsable(player);
        ClearPlayer(player);

        if (isSend)
        {
            _view.ShowQuickText(player, direction);
        }
    }

    /// <summary>
    /// 解除キーが押された。方向キーを押している間なら、候補を閉じて、キーを全部離すまで何も送らない
    /// </summary>
    /// <param name="player">押したプレイヤー</param>
    private void OnDefusePressed(int player)
    {
        if (_heldDirections[player].Count == 0 || _isCancelled[player])
        {
            return;
        }

        _isCancelled[player] = true;
        CloseWheel(player);
    }

    /// <summary>
    /// 今選んでいる方向（いちばん後に押して、まだ押している方向）を返す
    /// </summary>
    /// <param name="player">プレイヤー</param>
    /// <returns>選んでいる方向</returns>
    private InputDirection SelectedDirection(int player)
    {
        List<InputDirection> held = _heldDirections[player];
        return held[held.Count - 1];
    }

    /// <summary>
    /// クイックテキストを使える状態か
    /// </summary>
    /// <param name="player">プレイヤー</param>
    /// <returns>使えるなら true</returns>
    private bool IsUsable(int player)
    {
        if (_canUse != null)
        {
            return _canUse(player);
        }

        return _view != null && _view.CanUseQuickText(player);
    }

    /// <summary>
    /// 出ている候補を閉じる
    /// </summary>
    /// <param name="player">プレイヤー</param>
    private void CloseWheel(int player)
    {
        if (!_isWheelOpen[player])
        {
            return;
        }

        _isWheelOpen[player] = false;

        if (_view != null)
        {
            _view.SetQuickTextWheel(player, false, InputDirection.Up);
        }
    }

    /// <summary>
    /// 押している記録を消して、候補を閉じる
    /// </summary>
    /// <param name="player">プレイヤー</param>
    private void ClearPlayer(int player)
    {
        CloseWheel(player);
        _heldDirections[player].Clear();
        _heldSeconds[player] = 0f;
        _isCancelled[player] = false;
    }
}
