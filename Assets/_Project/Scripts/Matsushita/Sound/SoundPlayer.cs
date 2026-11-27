using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 効果音とBGMを鳴らす係。ゲームの起動時に自動で1つだけ作られ、シーンが変わっても残る。
/// どこからでも SoundPlayer.PlaySe(SoundId.Button) のように呼べる。シーンに置く必要はない
/// </summary>
public class SoundPlayer : MonoBehaviour
{
    private const string LibraryPath = "Sound/SoundLibrary";
    private const string TitleSceneName = "Title";
    private const string GameSceneName = "Game";
    private const int SeSourceCount = 8;

    private static SoundPlayer _instance;

    private SoundLibrary _library;
    private AudioSource _bgmSource;
    private AudioSource _loopSource;
    private AudioSource[] _seSources;
    private int _nextSeIndex;
    private bool _hasBgm;
    private SoundId _currentBgm;
    private bool _hasLoop;
    private SoundId _currentLoop;

    /// <summary>
    /// Play Mode に入るたびに前回の値を消す（ドメインのリロードを切っているため）
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        _instance = null;
    }

    /// <summary>
    /// 最初のシーンを読み込む前に、自分を作る
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        GameObject playerObject = new GameObject(nameof(SoundPlayer));
        DontDestroyOnLoad(playerObject);
        playerObject.AddComponent<SoundPlayer>();
    }

    /// <summary>
    /// 効果音を1回鳴らす
    /// </summary>
    /// <param name="id">音の種類</param>
    public static void PlaySe(SoundId id)
    {
        if (_instance != null)
        {
            _instance.PlaySeInternal(id);
        }
    }

    /// <summary>
    /// BGMを切り替える。同じ曲が流れていれば何もしない
    /// </summary>
    /// <param name="id">BGMの種類</param>
    public static void PlayBgm(SoundId id)
    {
        if (_instance != null)
        {
            _instance.PlayBgmInternal(id);
        }
    }

    /// <summary>
    /// BGMを止める
    /// </summary>
    public static void StopBgm()
    {
        if (_instance != null)
        {
            _instance._bgmSource.Stop();
            _instance._hasBgm = false;
        }
    }

    /// <summary>
    /// 効果音をループで鳴らし続ける（心拍など）。同時に鳴らせるループは1つ
    /// </summary>
    /// <param name="id">音の種類</param>
    public static void PlayLoop(SoundId id)
    {
        if (_instance != null)
        {
            _instance.PlayLoopInternal(id);
        }
    }

    /// <summary>
    /// ループの効果音を止める
    /// </summary>
    public static void StopLoop()
    {
        if (_instance != null)
        {
            _instance._loopSource.Stop();
            _instance._hasLoop = false;
        }
    }

    /// <summary>
    /// 音の長さを返す
    /// </summary>
    /// <param name="id">音の種類</param>
    /// <returns>長さ（秒）。音が入っていなければ 0</returns>
    public static float GetLength(SoundId id)
    {
        if (_instance != null && _instance.TryGetEntry(id, out SoundEntry entry))
        {
            return entry.Clip.length;
        }

        return 0f;
    }

    /// <summary>
    /// 対応表を読み込み、音を鳴らす AudioSource を用意する
    /// </summary>
    private void Awake()
    {
        _instance = this;
        _library = Resources.Load<SoundLibrary>(LibraryPath);

        if (_library == null)
        {
            Debug.LogWarning($"SoundPlayer: Resources/{LibraryPath} が見つからないので、音は鳴りません（メニューの Tools > サウンド から作れます）");
        }

        _bgmSource = CreateSource(true);
        _loopSource = CreateSource(true);
        _seSources = new AudioSource[SeSourceCount];

        for (int i = 0; i < SeSourceCount; i++)
        {
            _seSources[i] = CreateSource(false);
        }
    }

    /// <summary>
    /// シーンが読み込まれたことを受け取れるようにする
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// シーンが読み込まれたことを受け取るのをやめる
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// シーンに合わせてBGMを切り替える。ループの効果音は止める
    /// </summary>
    /// <param name="scene">読み込まれたシーン</param>
    /// <param name="mode">読み込み方</param>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
        {
            return;
        }

        StopLoop();

        if (scene.name == TitleSceneName)
        {
            PlayBgmInternal(SoundId.BgmTitle);
        }
        else if (scene.name == GameSceneName)
        {
            PlayBgmInternal(SoundId.BgmBattle);
        }
    }

    /// <summary>
    /// 空いている AudioSource で効果音を鳴らす（全部使っていれば一番古いものを使い回す）
    /// </summary>
    /// <param name="id">音の種類</param>
    private void PlaySeInternal(SoundId id)
    {
        if (!TryGetEntry(id, out SoundEntry entry))
        {
            return;
        }

        AudioSource source = _seSources[_nextSeIndex];
        _nextSeIndex = (_nextSeIndex + 1) % _seSources.Length;
        source.clip = entry.Clip;
        source.volume = entry.Volume;
        source.Play();
    }

    /// <summary>
    /// BGMを切り替える
    /// </summary>
    /// <param name="id">BGMの種類</param>
    private void PlayBgmInternal(SoundId id)
    {
        if (_hasBgm && _currentBgm == id && _bgmSource.isPlaying)
        {
            return;
        }

        _hasBgm = PlayOnSource(_bgmSource, id);
        _currentBgm = id;
    }

    /// <summary>
    /// ループの効果音を切り替える
    /// </summary>
    /// <param name="id">音の種類</param>
    private void PlayLoopInternal(SoundId id)
    {
        if (_hasLoop && _currentLoop == id && _loopSource.isPlaying)
        {
            return;
        }

        _hasLoop = PlayOnSource(_loopSource, id);
        _currentLoop = id;
    }

    /// <summary>
    /// ループ用の AudioSource で音を鳴らし始める
    /// </summary>
    /// <param name="source">鳴らす AudioSource</param>
    /// <param name="id">音の種類</param>
    /// <returns>鳴らせたら true</returns>
    private bool PlayOnSource(AudioSource source, SoundId id)
    {
        if (!TryGetEntry(id, out SoundEntry entry))
        {
            source.Stop();
            return false;
        }

        source.clip = entry.Clip;
        source.volume = entry.Volume;
        source.Play();
        return true;
    }

    /// <summary>
    /// 対応表から音を探す
    /// </summary>
    /// <param name="id">音の種類</param>
    /// <param name="entry">見つかった設定</param>
    /// <returns>音ファイルが入っていれば true</returns>
    private bool TryGetEntry(SoundId id, out SoundEntry entry)
    {
        if (_library == null)
        {
            entry = null;
            return false;
        }

        return _library.TryGet(id, out entry);
    }

    /// <summary>
    /// 自分に AudioSource を足す
    /// </summary>
    /// <param name="isLoop">ループさせるか</param>
    /// <returns>作った AudioSource</returns>
    private AudioSource CreateSource(bool isLoop)
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = isLoop;
        return source;
    }
}
