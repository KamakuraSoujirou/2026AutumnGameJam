using UnityEngine;

/// <summary>
/// 背景の左の机にあるPC画面を、ときどき起動する。待機（ランダムな間隔）→ 起動（ちらついて点く）→ 点灯（かすかにちらつく）→ 消灯、を繰り返す。
/// PC画面の絵（SpriteRenderer）の透明度だけを動かす
/// </summary>
public class UiPcScreenBoot : MonoBehaviour
{
    /// <summary>
    /// 起動の動き
    /// </summary>
    private enum BootStyle
    {
        Flicker,
        Fade,
        Cut
    }

    /// <summary>
    /// 今の段階
    /// </summary>
    private enum Phase
    {
        Wait,
        Boot,
        On,
        Off
    }

    private const float FadeBootSeconds = 0.7f;
    private const float CutBootSeconds = 0.05f;
    private const float OffSeconds = 0.3f;

    /// <summary>ちらついて点くときの、（始まりからの秒, 明るさ）</summary>
    private static readonly Vector2[] FlickerSteps =
    {
        new Vector2(0f, 1f), new Vector2(0.08f, 0f), new Vector2(0.16f, 1f), new Vector2(0.24f, 0.15f),
        new Vector2(0.34f, 1f), new Vector2(0.42f, 0.5f), new Vector2(0.55f, 1f)
    };

    [SerializeField, Tooltip("PC画面の絵。空なら子から探す")] private SpriteRenderer _target;
    [SerializeField, Tooltip("起動の動き（ちらついて点く・じわっと点く・ぱっと点く）")] private BootStyle _bootStyle = BootStyle.Flicker;
    [SerializeField, Tooltip("最初に点くまでの秒数")] private float _firstWaitSeconds = 2.5f;
    [SerializeField, Tooltip("次に点くまでの、短いほうの秒数")] private float _minWaitSeconds = 8f;
    [SerializeField, Tooltip("次に点くまでの、長いほうの秒数")] private float _maxWaitSeconds = 20f;
    [SerializeField, Tooltip("点いている秒数")] private float _holdSeconds = 4f;
    [SerializeField, Tooltip("オフにすると、画面は消えたままで起動しない")] private bool _isRunning = true;

    private Phase _phase = Phase.Wait;
    private float _phaseTime;
    private float _waitSeconds;

    /// <summary>
    /// 起動を繰り返すか（false にすると、点いていれば消えて、そのまま待つ）
    /// </summary>
    public bool IsRunning
    {
        get => _isRunning;
        set => _isRunning = value;
    }

    /// <summary>
    /// 今すぐ起動する（点灯中なら、点灯の時間を数え直す）
    /// </summary>
    public void BootNow()
    {
        SetPhase(Phase.Boot);
    }

    /// <summary>
    /// 絵を探して、最初の待ち時間を決める
    /// </summary>
    private void Awake()
    {
        if (_target == null)
        {
            _target = GetComponentInChildren<SpriteRenderer>(true);
        }

        _waitSeconds = _firstWaitSeconds;
        SetOpacity(0f);
    }

    /// <summary>
    /// 段階を進めて、透明度を合わせる
    /// </summary>
    private void Update()
    {
        if (_target == null)
        {
            return;
        }

        _phaseTime += Time.deltaTime;

        switch (_phase)
        {
            case Phase.Wait:
                SetOpacity(0f);

                if (_isRunning && _phaseTime >= _waitSeconds)
                {
                    SetPhase(Phase.Boot);
                }

                break;

            case Phase.Boot:
                SetOpacity(GetBootOpacity(_phaseTime));

                if (_phaseTime >= GetBootSeconds())
                {
                    SetPhase(Phase.On);
                }

                break;

            case Phase.On:
                // かすかにちらつく
                float time = Time.time;
                SetOpacity(0.92f + 0.08f * Mathf.Sin(time * 14f) * Mathf.Sin(time * 4.3f));

                if (_phaseTime >= _holdSeconds)
                {
                    SetPhase(Phase.Off);
                }

                break;

            default:
                SetOpacity(Mathf.Max(0f, 1f - _phaseTime / OffSeconds));

                if (_phaseTime >= OffSeconds)
                {
                    _waitSeconds = Random.Range(Mathf.Min(_minWaitSeconds, _maxWaitSeconds), Mathf.Max(_minWaitSeconds, _maxWaitSeconds));
                    SetPhase(Phase.Wait);
                }

                break;
        }
    }

    /// <summary>
    /// 段階を切り替える
    /// </summary>
    /// <param name="phase">次の段階</param>
    private void SetPhase(Phase phase)
    {
        _phase = phase;
        _phaseTime = 0f;
    }

    /// <summary>
    /// 起動にかかる秒数
    /// </summary>
    /// <returns>秒数</returns>
    private float GetBootSeconds()
    {
        switch (_bootStyle)
        {
            case BootStyle.Cut:
                return CutBootSeconds;
            case BootStyle.Fade:
                return FadeBootSeconds;
            default:
                return FlickerSteps[FlickerSteps.Length - 1].x;
        }
    }

    /// <summary>
    /// 起動の途中の明るさを返す
    /// </summary>
    /// <param name="seconds">起動を始めてからの秒数</param>
    /// <returns>明るさ（0〜1）</returns>
    private float GetBootOpacity(float seconds)
    {
        switch (_bootStyle)
        {
            case BootStyle.Cut:
                return 1f;
            case BootStyle.Fade:
                return Mathf.Clamp01(seconds / FadeBootSeconds);
            default:
                for (int i = FlickerSteps.Length - 1; i >= 0; i--)
                {
                    if (seconds >= FlickerSteps[i].x)
                    {
                        return FlickerSteps[i].y;
                    }
                }

                return 0f;
        }
    }

    /// <summary>
    /// 絵の透明度を変える
    /// </summary>
    /// <param name="opacity">透明度（0〜1）</param>
    private void SetOpacity(float opacity)
    {
        if (_target == null)
        {
            return;
        }

        Color color = _target.color;
        color.a = opacity;
        _target.color = color;
    }
}
