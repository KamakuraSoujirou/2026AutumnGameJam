using TMPro;
using UnityEngine;

/// <summary>
/// リザルト画面のキャラ1人分（勝ち／負けの見た目、「WIN」「LOSE」の文字、名前）
/// </summary>
public class UiResultPlayer : MonoBehaviour
{
    private const float CharacterSlideDistance = 67f;
    private const float WinTagStartScale = 1.9f;
    private const float LoseTagDropDistance = 48f;

    [SerializeField, Tooltip("勝ったときの見た目（勝ち顔）")] private GameObject _winVisual;
    [SerializeField, Tooltip("負けたときの見た目（アフロ）")] private GameObject _loseVisual;
    [SerializeField, Tooltip("勝ったときに出す「WIN」の文字")] private GameObject _winTag;
    [SerializeField, Tooltip("負けたときに出す「LOSE」の文字")] private GameObject _loseTag;
    [SerializeField, Tooltip("プレイヤー名")] private TMP_Text _nameText;

    private RectTransform _characterRect;
    private RectTransform _nameRect;
    private CanvasGroup _characterGroup;
    private CanvasGroup _nameGroup;
    private Vector2 _characterBasePosition;
    private Vector2 _nameBasePosition;
    private Vector2 _winTagBasePosition;
    private Vector2 _loseTagBasePosition;
    private bool _isWinner;
    private bool _isCached;

    /// <summary>
    /// 勝ち負けと名前を表示に反映する
    /// </summary>
    /// <param name="isWinner">勝った側なら true</param>
    /// <param name="playerName">プレイヤー名</param>
    public void Show(bool isWinner, string playerName)
    {
        _isWinner = isWinner;
        _winVisual.SetActive(isWinner);
        _loseVisual.SetActive(!isWinner);
        _winTag.SetActive(isWinner);
        _loseTag.SetActive(!isWinner);
        _nameText.text = playerName;
    }

    /// <summary>
    /// キャラと名前が下から出てくる動きを進める
    /// </summary>
    /// <param name="progress">進み具合（0：出る前、1：出きった）</param>
    public void SetCharacterProgress(float progress)
    {
        Cache();

        float eased = UiResultEase.OutCubic(progress);
        Vector2 offset = new Vector2(0f, -CharacterSlideDistance * (1f - eased));
        float alpha = Mathf.Clamp01(progress * 2.2f);

        _characterRect.anchoredPosition = _characterBasePosition + offset;
        _characterGroup.alpha = alpha;
        _nameRect.anchoredPosition = _nameBasePosition + offset;
        _nameGroup.alpha = alpha;
    }

    /// <summary>
    /// 「WIN」は大きくポンと、「LOSE」は上から落ちる動きを進める
    /// </summary>
    /// <param name="progress">進み具合（0：出る前、1：出きった）</param>
    public void SetTagProgress(float progress)
    {
        Cache();

        GameObject tag = _isWinner ? _winTag : _loseTag;
        RectTransform rect = (RectTransform)tag.transform;

        if (_isWinner)
        {
            // 文字の左上が基準なので、真ん中を中心に大きさが変わるよう位置をずらす
            float scale = Mathf.LerpUnclamped(WinTagStartScale, 1f, UiResultEase.OutBack(progress));
            Vector2 shift = new Vector2(-rect.sizeDelta.x * 0.5f * (scale - 1f), rect.sizeDelta.y * 0.5f * (scale - 1f));
            rect.localScale = Vector3.one * scale;
            rect.anchoredPosition = _winTagBasePosition + shift;
        }
        else
        {
            rect.anchoredPosition = _loseTagBasePosition + new Vector2(0f, LoseTagDropDistance * (1f - UiResultEase.InQuad(progress)));
        }

        GetGroup(tag).alpha = Mathf.Clamp01(progress * 3f);
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
        _characterRect = (RectTransform)_winVisual.transform.parent;
        _nameRect = _nameText.rectTransform;
        _characterGroup = GetGroup(_characterRect.gameObject);
        _nameGroup = GetGroup(_nameRect.gameObject);
        _characterBasePosition = _characterRect.anchoredPosition;
        _nameBasePosition = _nameRect.anchoredPosition;
        _winTagBasePosition = ((RectTransform)_winTag.transform).anchoredPosition;
        _loseTagBasePosition = ((RectTransform)_loseTag.transform).anchoredPosition;
    }

    /// <summary>
    /// 透明度を変える部品を返す（なければ付ける）
    /// </summary>
    /// <param name="target">付ける先</param>
    /// <returns>透明度の部品</returns>
    private static CanvasGroup GetGroup(GameObject target)
    {
        if (!target.TryGetComponent(out CanvasGroup group))
        {
            group = target.AddComponent<CanvasGroup>();
        }

        return group;
    }
}
