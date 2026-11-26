using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲームの調整値をまとめた設定。Assets/Resources/Config/GameConfig.json から読む
/// 値の意味と初期値は Notion「データと調整パラメータ」と同じ
/// </summary>
/// <remarks>
/// JsonUtility は変数名を JSON のキーとして使うため、フィールドだけは _ を付けずに JSON と同じ名前にしている。
/// JSON に書かれていない値は、ここに書いた初期値のままになる。
/// </remarks>
[Serializable]
public class GameConfig
{
    /// <summary>Resources からの JSON のパス（拡張子なし）</summary>
    public const string ResourcePath = "Config/GameConfig";

    private static readonly int[] DefaultStagePasses = { 1, 3, 5, 7 };

    private static GameConfig _loaded;

    [SerializeField] private float holdTime = 10f;
    [SerializeField] private float warningTime = 3f;
    [SerializeField] private float missPenalty = 2f;
    [SerializeField] private float stunTime = 0.5f;
    [SerializeField] private float flightTime = 0.4f;
    [SerializeField] private float arriveLock = 0.15f;
    [SerializeField] private float introTime = 1.8f;
    [SerializeField] private float boomTime = 2f;
    [SerializeField] private int roundsToWin = 2;
    [SerializeField] private int[] stagePasses = DefaultStagePasses;
    [SerializeField] private int defuseMinPass = 5;
    [SerializeField] private bool isShrinkEnabled = true;
    [SerializeField] private int shrinkFromPass = 10;
    [SerializeField] private float shrinkMin = 5f;
    [SerializeField] private float lampInterval = 0.6f;
    [SerializeField] private float idleReturn = 30f;

    /// <summary>受け取るたびに戻す持ち時間（秒）。Notion の HOLD_TIME</summary>
    public float TimeLimit => holdTime;

    /// <summary>残り何秒から警告の演出を出すか</summary>
    public float WarningTime => warningTime;

    /// <summary>不正解で減る時間（秒）。Notion の MISS_PENALTY</summary>
    public float WrongPenalty => missPenalty;

    /// <summary>不正解のあと入力できない時間（秒）。Notion の STUN_TIME</summary>
    public float StunDuration => stunTime;

    /// <summary>爆弾が相手に飛んでいく長さ（秒）。Notion の FLIGHT_TIME</summary>
    public float FlightDuration => flightTime;

    /// <summary>受け取った直後に入力できない時間（秒）。Notion の ARRIVE_LOCK</summary>
    public float ReceiveLockDuration => arriveLock;

    /// <summary>ラウンド開始の演出の長さ（秒）。Notion の INTRO_TIME</summary>
    public float RoundStartDuration => introTime;

    /// <summary>爆発の演出の長さ（秒）。Notion の BOOM_TIME</summary>
    public float ExplosionDuration => boomTime;

    /// <summary>何ラウンド取れば試合に勝つか。Notion の ROUNDS_TO_WIN</summary>
    public int WinsToMatch => roundsToWin;

    /// <summary>難易度の各段階（1〜4）が始まる受け取り回数。Notion の STAGE_PASSES</summary>
    public IReadOnlyList<int> StagePasses => stagePasses;

    /// <summary>解除が使えるパス回数。Notion の DEFUSE_MIN_PASS</summary>
    public int DefuseMinPass => defuseMinPass;

    /// <summary>時間短縮を使うか</summary>
    public bool IsShortenEnabled => isShrinkEnabled;

    /// <summary>何回目のパスから持ち時間を短くするか。Notion の SHRINK_FROM_PASS</summary>
    public int ShortenStartPass => shrinkFromPass;

    /// <summary>短くしたときの持ち時間の下限（秒）。Notion の SHRINK_MIN</summary>
    public float MinTimeLimit => shrinkMin;

    /// <summary>ギミックDのランプが色を変える間隔（秒）。Notion の LAMP_INTERVAL</summary>
    public float LampInterval => lampInterval;

    /// <summary>展示用：無操作でタイトルに戻るまでの時間（秒）。Notion の IDLE_RETURN</summary>
    public float IdleReturnTime => idleReturn;

    /// <summary>
    /// JSON から設定を読む。2回目からは最初に読んだものを返す
    /// 読めなかったときはエラーを出して、初期値の設定を返す
    /// </summary>
    /// <returns>ゲームの設定</returns>
    public static GameConfig Load()
    {
        if (_loaded == null)
        {
            _loaded = LoadFromResources();
        }

        return _loaded;
    }

    /// <summary>
    /// Play Mode を始めるたびに読み直す（ドメインのリロードを切っていても、JSON の変更が反映されるように）
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearLoaded()
    {
        _loaded = null;
    }

    /// <summary>
    /// Resources の JSON を読み、初期値の上に書かれている値だけを上書きする
    /// </summary>
    /// <returns>読んだ設定</returns>
    private static GameConfig LoadFromResources()
    {
        GameConfig config = new GameConfig();
        TextAsset textAsset = Resources.Load<TextAsset>(ResourcePath);

        if (textAsset == null)
        {
            Debug.LogError($"GameConfig: Resources/{ResourcePath}.json が見つからないので、初期値を使います");
            return config;
        }

        try
        {
            JsonUtility.FromJsonOverwrite(textAsset.text, config);
        }
        catch (ArgumentException exception)
        {
            Debug.LogError($"GameConfig: {ResourcePath}.json の書き方が間違っているので、初期値を使います（カンマや括弧を確認）\n{exception.Message}");
            return new GameConfig();
        }

        if (config.stagePasses == null || config.stagePasses.Length == 0)
        {
            Debug.LogWarning("GameConfig: stagePasses が空なので、1・3・5・7 を使います");
            config.stagePasses = DefaultStagePasses;
        }

        return config;
    }
}
