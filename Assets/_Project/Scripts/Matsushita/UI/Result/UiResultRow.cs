using TMPro;
using UnityEngine;

/// <summary>
/// リザルトの表の1行（ラウンド・仕組み・取った側の★・決着・パス回数）
/// </summary>
public class UiResultRow : MonoBehaviour
{
    private const float SlideDistance = 86f;
    private const float StarStartProgress = 0.35f;

    [SerializeField, Tooltip("ラウンドの番号")] private TMP_Text _roundText;
    [SerializeField, Tooltip("仕組み（ギミック）の名前")] private TMP_Text _gimmickText;
    [SerializeField, Tooltip("P1がラウンドを取ったときに出す★")] private GameObject _player1Star;
    [SerializeField, Tooltip("P2がラウンドを取ったときに出す★")] private GameObject _player2Star;
    [SerializeField, Tooltip("決着（爆発か解除）")] private TMP_Text _resultText;
    [SerializeField, Tooltip("パス回数")] private TMP_Text _passText;

    private RectTransform _rect;
    private CanvasGroup _group;
    private Vector2 _basePosition;
    private bool _isCached;

    /// <summary>
    /// この行を表示する
    /// </summary>
    /// <param name="round">ラウンドの番号（1から）</param>
    /// <param name="result">そのラウンドの結果</param>
    public void Show(int round, UiResultRound result)
    {
        gameObject.SetActive(true);
        _roundText.text = round.ToString();
        _gimmickText.text = result.GimmickName;
        _player1Star.SetActive(result.WinnerIndex == 0);
        _player2Star.SetActive(result.WinnerIndex == 1);
        _resultText.text = result.IsDefused ? "解除" : "爆発";
        _passText.text = $"{result.PassCount:00}回";
    }

    /// <summary>
    /// この行を隠す（ラウンドが少ないとき）
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 左から出てきて、少し遅れて★がポンと出る動きを進める
    /// </summary>
    /// <param name="progress">進み具合（0：出る前、1：出きった）</param>
    public void SetProgress(float progress)
    {
        Cache();

        _rect.anchoredPosition = _basePosition + new Vector2(-SlideDistance * (1f - UiResultEase.OutCubic(progress)), 0f);
        _group.alpha = Mathf.Clamp01(progress * 3.3f);

        float starProgress = Mathf.Clamp01((progress - StarStartProgress) / (1f - StarStartProgress));
        float starScale = starProgress <= 0f ? 0f : UiResultEase.OutBack(starProgress);
        _player1Star.transform.localScale = Vector3.one * starScale;
        _player2Star.transform.localScale = Vector3.one * starScale;
    }

    /// <summary>
    /// 元の位置を覚えて、透明にするための部品を付ける（最初に1回だけ）
    /// </summary>
    private void Cache()
    {
        if (_isCached)
        {
            return;
        }

        _isCached = true;
        _rect = (RectTransform)transform;

        if (!TryGetComponent(out _group))
        {
            _group = gameObject.AddComponent<CanvasGroup>();
        }

        _basePosition = _rect.anchoredPosition;
    }
}
