using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 試合の進み方（BombManager と BombTimer のイベント）に合わせて効果音とBGMを鳴らす。
/// シーンに BombManager があれば自動で同じオブジェクトに付くので、シーンに置く必要はない
/// </summary>
[RequireComponent(typeof(BombManager))]
public class BombSoundHooks : MonoBehaviour
{
    private const float FunnyDelay = 0.6f;

    private BombManager _bombManager;
    private BombTimer _bombTimer;
    private RoundGimmickSwitcher _gimmickSwitcher;
    private Wiringgimmick _wireGimmick;
    private bool _hasLastState;
    private BombState _lastState;
    private int _lastWholeSecond;

    /// <summary>
    /// 最初のシーンを読み込む前に、シーンの読み込みを受け取れるようにする
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// 読み込まれたシーンに BombManager があれば、自分を付ける（シーンの読み込みごとに1回だけ探す）
    /// </summary>
    /// <param name="scene">読み込まれたシーン</param>
    /// <param name="mode">読み込み方</param>
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BombManager bombManager = FindFirstObjectByType<BombManager>();

        if (bombManager != null && !bombManager.TryGetComponent(out BombSoundHooks _))
        {
            bombManager.gameObject.AddComponent<BombSoundHooks>();
        }
    }

    /// <summary>
    /// 進行役とタイマー、ギミックを取得しておく
    /// </summary>
    private void Awake()
    {
        _bombManager = GetComponent<BombManager>();
        _bombTimer = FindFirstObjectByType<BombTimer>();
        _gimmickSwitcher = FindFirstObjectByType<RoundGimmickSwitcher>();
        _wireGimmick = FindFirstObjectByType<Wiringgimmick>(FindObjectsInactive.Include);
    }

    /// <summary>
    /// 進行役とタイマーのイベントを受け取れるようにする
    /// </summary>
    private void OnEnable()
    {
        _bombManager.OnChanged += HandleChanged;
        _bombManager.OnPressed += HandlePressed;
        _bombManager.OnMissed += HandleMissed;

        if (_bombTimer != null)
        {
            _bombTimer.OnWarning += HandleWarning;
        }
    }

    /// <summary>
    /// イベントを受け取るのをやめ、ループの音を止める
    /// </summary>
    private void OnDisable()
    {
        _bombManager.OnChanged -= HandleChanged;
        _bombManager.OnPressed -= HandlePressed;
        _bombManager.OnMissed -= HandleMissed;

        if (_bombTimer != null)
        {
            _bombTimer.OnWarning -= HandleWarning;
        }

        SoundPlayer.StopLoop();
    }

    /// <summary>
    /// 答えている間、残り時間が1秒減るたびにチクタクを鳴らす
    /// </summary>
    private void Update()
    {
        if (_bombTimer == null || _bombManager.State != BombState.Answering || !_bombTimer.IsRunning)
        {
            return;
        }

        int wholeSecond = Mathf.CeilToInt(_bombTimer.Remaining);

        if (wholeSecond < _lastWholeSecond)
        {
            SoundPlayer.PlaySe(SoundId.Tick);
        }

        _lastWholeSecond = wholeSecond;
    }

    /// <summary>
    /// 進行の状態が変わったときだけ、その状態の音を鳴らす
    /// </summary>
    private void HandleChanged()
    {
        BombState state = _bombManager.State;

        if (_hasLastState && state == _lastState)
        {
            return;
        }

        BombState previous = _lastState;
        bool hasPrevious = _hasLastState;
        _lastState = state;
        _hasLastState = true;
        StopAllCoroutines();

        switch (state)
        {
            case BombState.RoundStart:
                SoundPlayer.StopLoop();

                if (hasPrevious && previous == BombState.MatchOver)
                {
                    SoundPlayer.PlayBgm(SoundId.BgmBattle);
                }

                SoundPlayer.PlaySe(SoundId.RoundStart);
                break;
            case BombState.Answering:
                if (previous == BombState.Flying)
                {
                    SoundPlayer.PlaySe(SoundId.Land);
                }

                _lastWholeSecond = _bombTimer != null ? Mathf.CeilToInt(_bombTimer.Remaining) : 0;
                break;
            case BombState.Flying:
                SoundPlayer.StopLoop();
                break;
            case BombState.Exploding:
                SoundPlayer.StopLoop();
                SoundPlayer.PlaySe(SoundId.Explosion);
                StartCoroutine(PlayFunnyLater());
                break;
            case BombState.MatchOver:
                SoundPlayer.StopLoop();
                SoundPlayer.StopBgm();
                SoundPlayer.PlaySe(SoundId.Fanfare);
                StartCoroutine(PlayResultBgmLater(SoundPlayer.GetLength(SoundId.Fanfare)));
                break;
        }
    }

    /// <summary>
    /// ボタンが押されたときの音。配線ギミックのときは線を切る音にする
    /// </summary>
    /// <param name="direction">押された方向</param>
    private void HandlePressed(InputDirection direction)
    {
        SoundPlayer.PlaySe(IsWireGimmickActive() ? SoundId.WireCut : SoundId.Button);
    }

    /// <summary>
    /// 不正解の音
    /// </summary>
    /// <param name="player">間違えたプレイヤー</param>
    /// <param name="penalty">減る秒数</param>
    private void HandleMissed(int player, float penalty)
    {
        SoundPlayer.PlaySe(SoundId.Wrong);
    }

    /// <summary>
    /// 残りわずかになったら、心拍をループで鳴らす（受け取りで時間が戻るか、爆発したら止める）
    /// </summary>
    private void HandleWarning()
    {
        if (_bombManager.State == BombState.Answering)
        {
            SoundPlayer.PlayLoop(SoundId.Heartbeat);
        }
    }

    /// <summary>
    /// 今のギミックが配線ギミックか調べる
    /// </summary>
    /// <returns>配線ギミックなら true</returns>
    private bool IsWireGimmickActive()
    {
        if (_gimmickSwitcher != null)
        {
            return _gimmickSwitcher.Current is Wiringgimmick;
        }

        return _wireGimmick != null && _wireGimmick.isActiveAndEnabled;
    }

    /// <summary>
    /// 爆発の少しあとに、間抜けな音を鳴らす
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator PlayFunnyLater()
    {
        yield return new WaitForSeconds(FunnyDelay);
        SoundPlayer.PlaySe(SoundId.Funny);
    }

    /// <summary>
    /// ファンファーレが終わってから、リザルトのBGMを流す
    /// </summary>
    /// <param name="delay">待つ秒数</param>
    /// <returns>コルーチン</returns>
    private IEnumerator PlayResultBgmLater(float delay)
    {
        yield return new WaitForSeconds(delay);
        SoundPlayer.PlayBgm(SoundId.BgmResult);
    }
}
