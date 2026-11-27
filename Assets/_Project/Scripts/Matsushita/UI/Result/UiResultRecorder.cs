using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 試合の進み方（BombManager の知らせ）を見てラウンドごとの結果を集め、試合が終わったらリザルト画面を重ねて出す。
/// シーンに BombManager があれば自動で同じオブジェクトに付くので、シーンに置く必要はない
/// </summary>
/// <remarks>
/// 再戦は結果を閉じて BombManager.TryRematch を呼ぶ。HOME は Title シーンを読み込む。
/// ラウンドは爆発（Exploding）か解除成功（Defused）で終わるので、どちらで決着したかも記録する
/// </remarks>
[RequireComponent(typeof(BombManager))]
public class UiResultRecorder : MonoBehaviour
{
    private const string ResultPrefabPath = "UI/UI_Result";
    private const string TitleSceneName = "Title";
    private const string FallbackGimmickName = "テスト";
    private const int ResultSortingOrder = 100;

    private readonly List<UiResultRound> _rounds = new List<UiResultRound>();

    private BombManager _bombManager;
    private UiResultView _view;
    private BombState? _lastState;

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

        if (bombManager != null && !bombManager.TryGetComponent(out UiResultRecorder _))
        {
            bombManager.gameObject.AddComponent<UiResultRecorder>();
        }
    }

    /// <summary>
    /// 進行役を取得しておく
    /// </summary>
    private void Awake()
    {
        _bombManager = GetComponent<BombManager>();
    }

    /// <summary>
    /// 試合の進み方の知らせを受け取れるようにする（BombManager の Start より先に登録される）
    /// </summary>
    private void OnEnable()
    {
        _bombManager.OnChanged += OnChanged;
    }

    /// <summary>
    /// 試合の進み方の知らせを受け取るのをやめる
    /// </summary>
    private void OnDisable()
    {
        _bombManager.OnChanged -= OnChanged;
    }

    /// <summary>
    /// 状態が切り替わったときだけ、結果の記録・リザルトの表示と片付けを行う
    /// </summary>
    private void OnChanged()
    {
        BombState state = _bombManager.State;

        if (_lastState == state)
        {
            return;
        }

        _lastState = state;

        switch (state)
        {
            case BombState.RoundStart:
                OnRoundStart();
                break;
            case BombState.Exploding:
                RecordRound(false);
                break;
            case BombState.Defused:
                RecordRound(true);
                break;
            case BombState.MatchOver:
                ShowResult();
                break;
        }
    }

    /// <summary>
    /// ラウンドが始まった。試合の最初なら記録を空にして、リザルトを閉じる
    /// </summary>
    private void OnRoundStart()
    {
        if (_bombManager.GetWins(0) + _bombManager.GetWins(1) == 0)
        {
            _rounds.Clear();
        }

        CloseResult();
    }

    /// <summary>
    /// 終わったラウンドの結果を記録する（取った人は BombManager が決めている）
    /// </summary>
    /// <param name="isDefused">解除成功で決着したなら true、爆発なら false</param>
    private void RecordRound(bool isDefused)
    {
        string gimmickName = string.IsNullOrEmpty(_bombManager.GimmickName) ? FallbackGimmickName : _bombManager.GimmickName;
        _rounds.Add(new UiResultRound(gimmickName, _bombManager.RoundWinner, isDefused, _bombManager.ReceiveCount));
    }

    /// <summary>
    /// 試合が終わったので、集めた結果でリザルト画面を出す
    /// </summary>
    private void ShowResult()
    {
        if (_view == null && !CreateView())
        {
            return;
        }

        _view.gameObject.SetActive(true);
        _view.Show(_bombManager.MatchWinner, "P1", CpuSettings.IsCpuMode ? "COM" : "P2", _rounds.ToArray());
    }

    /// <summary>
    /// リザルト画面を作って、試合画面より手前に置く
    /// </summary>
    /// <returns>作れたら true</returns>
    private bool CreateView()
    {
        GameObject prefab = Resources.Load<GameObject>(ResultPrefabPath);

        if (prefab == null)
        {
            Debug.LogError($"[UiResultRecorder] Resources/{ResultPrefabPath} が見つかりません。メニューの Tools → UI仮組 → リザルト画面を作る を実行してください");
            return false;
        }

        GameObject instance = Instantiate(prefab);
        instance.GetComponent<Canvas>().sortingOrder = ResultSortingOrder;
        _view = instance.GetComponent<UiResultView>();
        _view.RestartRequested += OnRestartRequested;
        _view.TitleRequested += OnTitleRequested;
        return true;
    }

    /// <summary>
    /// リザルト画面を閉じる
    /// </summary>
    private void CloseResult()
    {
        if (_view != null)
        {
            _view.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 再戦が選ばれたので、リザルトを閉じてもう一度試合を始める
    /// </summary>
    private void OnRestartRequested()
    {
        CloseResult();
        _bombManager.TryRematch();
    }

    /// <summary>
    /// HOME が選ばれたので、タイトルへ戻る
    /// </summary>
    private void OnTitleRequested()
    {
        SceneManager.LoadScene(TitleSceneName);
    }
}
