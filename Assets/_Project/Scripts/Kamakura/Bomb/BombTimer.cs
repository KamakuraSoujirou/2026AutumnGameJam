using System;
using UnityEngine;

/// <summary>
/// 爆弾の残り時間を数える。止める・進めるのタイミングは進行役（BombManager）が決める
/// </summary>
public class BombTimer : MonoBehaviour
{
    private GameConfig _config;
    private float _remaining;
    private bool _isRunning;
    private bool _hasWarned;
    private bool _hasExploded;

    /// <summary>
    /// 残り時間が警告の秒数を下回ったときに1回だけ呼ばれる（残り3秒の演出用）
    /// </summary>
    public event Action OnWarning;

    /// <summary>
    /// 残り時間が0になったときに1回だけ呼ばれる
    /// </summary>
    public event Action OnExploded;

    /// <summary>
    /// 残り時間（秒）
    /// </summary>
    public float Remaining => _remaining;

    /// <summary>
    /// 時間が進んでいるか
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// 爆発したか
    /// </summary>
    public bool HasExploded => _hasExploded;

    /// <summary>
    /// 受け取るたびに戻す持ち時間（秒）
    /// </summary>
    public float TimeLimit => _config.TimeLimit;

    /// <summary>
    /// 残り何秒から警告にするか（秒）
    /// </summary>
    public float WarningTime => _config.WarningTime;

    /// <summary>
    /// 設定を読み、残り時間を持ち時間にしておく
    /// </summary>
    private void Awake()
    {
        _config = GameConfig.Load();
        _remaining = TimeLimit;
    }

    /// <summary>
    /// 動いているときだけ時間を減らす
    /// </summary>
    private void Update()
    {
        if (!_isRunning)
        {
            return;
        }

        _remaining -= Time.deltaTime;
        CheckRemaining();
    }

    /// <summary>
    /// 残り時間を持ち時間に戻す（止まったまま）
    /// </summary>
    public void ResetTime()
    {
        ResetTime(TimeLimit);
    }

    /// <summary>
    /// 残り時間を指定した秒数に戻す（止まったまま）。時間短縮のときに使う
    /// </summary>
    /// <param name="seconds">新しい残り時間</param>
    public void ResetTime(float seconds)
    {
        _remaining = Mathf.Max(0f, seconds);
        _isRunning = false;
        _hasWarned = _remaining <= WarningTime;
        _hasExploded = false;
    }

    /// <summary>
    /// 時間を進め始める。爆発したあとは動かない
    /// </summary>
    public void StartTimer()
    {
        if (_hasExploded)
        {
            return;
        }

        _isRunning = true;
    }

    /// <summary>
    /// 時間を止める
    /// </summary>
    public void StopTimer()
    {
        _isRunning = false;
    }

    /// <summary>
    /// 残り時間を減らす（不正解のペナルティ）。0を下回ったら爆発する
    /// </summary>
    /// <param name="seconds">減らす秒数</param>
    public void Subtract(float seconds)
    {
        if (_hasExploded)
        {
            return;
        }

        _remaining -= seconds;
        CheckRemaining();
    }

    /// <summary>
    /// 残り時間を見て、警告と爆発のイベントを出す
    /// </summary>
    private void CheckRemaining()
    {
        if (!_hasWarned && _remaining <= WarningTime)
        {
            _hasWarned = true;
            OnWarning?.Invoke();
        }

        if (_remaining <= 0f)
        {
            _remaining = 0f;
            _isRunning = false;
            _hasExploded = true;
            OnExploded?.Invoke();
        }
    }
}
