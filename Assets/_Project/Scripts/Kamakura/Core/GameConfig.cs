using UnityEngine;

/// <summary>
/// ゲームの調整値をまとめた設定。Projectウィンドウの Create > Game > GameConfig で作る
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Game/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("持ち時間")]
    [SerializeField, Tooltip("受け取るたびに戻す持ち時間（秒）")] private float _timeLimit = 10f;
    [SerializeField, Tooltip("残り何秒から警告の演出を出すか")] private float _warningTime = 3f;

    [Header("不正解")]
    [SerializeField, Tooltip("不正解で減る時間（秒）")] private float _wrongPenalty = 2f;
    [SerializeField, Tooltip("不正解のあと入力できない時間（秒）")] private float _stunDuration = 0.5f;

    [Header("進行")]
    [SerializeField, Tooltip("ラウンド開始の演出の長さ（秒）")] private float _roundStartDuration = 1.8f;
    [SerializeField, Tooltip("爆弾が相手に飛んでいく長さ（秒）")] private float _flightDuration = 0.4f;
    [SerializeField, Tooltip("爆発の演出の長さ（秒）")] private float _explosionDuration = 2f;
    [SerializeField, Tooltip("受け取った直後に入力できない時間（秒）")] private float _receiveLockDuration = 0.15f;
    [SerializeField, Tooltip("何ラウンド取れば試合に勝つか")] private int _winsToMatch = 2;

    [Header("時間短縮")]
    [SerializeField, Tooltip("時間短縮を使うか")] private bool _isShortenEnabled = true;
    [SerializeField, Tooltip("何回目のパスから短くするか")] private int _shortenStartPass = 10;
    [SerializeField, Tooltip("短くしたときの持ち時間の下限（秒）")] private float _minTimeLimit = 5f;

    /// <summary>受け取るたびに戻す持ち時間（秒）</summary>
    public float TimeLimit => _timeLimit;

    /// <summary>残り何秒から警告の演出を出すか</summary>
    public float WarningTime => _warningTime;

    /// <summary>不正解で減る時間（秒）</summary>
    public float WrongPenalty => _wrongPenalty;

    /// <summary>不正解のあと入力できない時間（秒）</summary>
    public float StunDuration => _stunDuration;

    /// <summary>ラウンド開始の演出の長さ（秒）</summary>
    public float RoundStartDuration => _roundStartDuration;

    /// <summary>爆弾が相手に飛んでいく長さ（秒）</summary>
    public float FlightDuration => _flightDuration;

    /// <summary>爆発の演出の長さ（秒）</summary>
    public float ExplosionDuration => _explosionDuration;

    /// <summary>受け取った直後に入力できない時間（秒）</summary>
    public float ReceiveLockDuration => _receiveLockDuration;

    /// <summary>何ラウンド取れば試合に勝つか</summary>
    public int WinsToMatch => _winsToMatch;

    /// <summary>時間短縮を使うか</summary>
    public bool IsShortenEnabled => _isShortenEnabled;

    /// <summary>何回目のパスから短くするか</summary>
    public int ShortenStartPass => _shortenStartPass;

    /// <summary>短くしたときの持ち時間の下限（秒）</summary>
    public float MinTimeLimit => _minTimeLimit;
}
